
using MyVidious.Access;
using MyVidious.Data;
using MyVidious.Models.Invidious;
using Quartz;

namespace MyVidious.Background; 

public class ChannelVideoJob : IJob
{
    private InvidiousAPIAccess _invidiousAPIAccess;
    private IServiceScopeFactory _serviceScopeFactory;
    private InvidiousUrlsAccess _invidiousUrlsAccess;
    private MeilisearchAccess _meilisearchAccess;

    public ChannelVideoJob(
        IServiceScopeFactory serviceScopeFactory, 
        InvidiousAPIAccess invidiousAPIAccess, 
        InvidiousUrlsAccess invidiousUrlsAccess,
        MeilisearchAccess meilisearchAccess)
    {
        _invidiousAPIAccess = invidiousAPIAccess;
        _serviceScopeFactory = serviceScopeFactory;
        _invidiousUrlsAccess = invidiousUrlsAccess;
        _meilisearchAccess = meilisearchAccess;
    }

    async Task IJob.Execute(IJobExecutionContext context)
    {
        await UpdateAllChannelVideos(context.CancellationToken);
    }

    private async Task UpdateAllChannelVideos(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var videoDbContext = scope.ServiceProvider.GetRequiredService<VideoDbContext>();
        var cutoff = DateTime.UtcNow.AddHours(-48);
        var incompleteChannels = videoDbContext.Channels.Where(z => z.DateLastScraped == null || !z.ScrapedToOldest).Take(10).ToList();
        var sortedChannels = incompleteChannels.Where(z => z.DateLastScraped == null).Concat(incompleteChannels.Where(z => z.DateLastScraped != null && (z.ScrapeFailureCount < 3 || z.DateLastScraped < cutoff)));
        foreach(var channel in sortedChannels)
        {
            await UpdateChannelVideos(stoppingToken, videoDbContext, channel);
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }

        var channelToUpdate = videoDbContext.Channels.Where(z => z.ScrapedToOldest && z.DateLastScraped < cutoff).ToList();
        foreach (var channel in channelToUpdate)
        {
            await UpdateChannelVideos(stoppingToken, videoDbContext, channel);
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task UpdateChannelVideos(CancellationToken stoppingToken, VideoDbContext videoDbContext, ChannelEntity channel)
    {
        var request = new Models.Invidious.ChannelVideosRequest
        {
            Sort_by = "newest"
        };
        while(true)
        {
            Models.Invidious.ChannelVideosResponse response;
            try
            {
                response = await _invidiousAPIAccess.GetChannelVideos(channel.UniqueId, request);
            }
            catch (Exception)
            {
                Console.WriteLine($"Failed to scrape videos for channel {channel.Name}");
                channel.ScrapeFailureCount++;
                channel.DateLastScraped = DateTime.UtcNow;
                videoDbContext.SaveChanges();
                return;
            }
            channel.ScrapeFailureCount = 0;//we reset this each time there's a success
            var uniqueIds = response.Videos.Select(z => z.VideoId).ToList();
            var existingVideos = videoDbContext.Videos.Where(z => uniqueIds.Contains(z.UniqueId)).ToList();
            //it's possible for a playlist to have already added the video, and therefore the channelId would be null for such videos
            var existingVideosWithoutChannel = existingVideos.Where(z => !z.ChannelId.HasValue).ToList();
            var anyExistingVideosWithChannel = existingVideos.Any(z => z.ChannelId.HasValue);//needed to later decide if need to keep scraping
            existingVideosWithoutChannel.ForEach(z => z.ChannelId = channel.Id);
            var existingUniqueIds = existingVideos.Select(z => z.UniqueId).ToList();
            var videosToAdd = response.Videos.Where(z => !existingUniqueIds.Contains(z.VideoId)).Select(TranslateToEntity).ToList();
            foreach(var video in videosToAdd)
            {
                video.Channel = channel;
            }
            videoDbContext.AddRange(videosToAdd);
            channel.VideoCount = channel.VideoCount + videosToAdd.Count + existingVideosWithoutChannel.Count;
            channel.DateLastScraped = DateTime.UtcNow;
            Console.WriteLine($"scraped {videosToAdd.Count} videos for channel {channel.Name}");
            videoDbContext.SaveChanges();
            await _meilisearchAccess.AddItems(videosToAdd.Select(z => new MeilisearchItem
            {
                VideoId = z.Id,
                Name = z.Title,
                SecondName = channel.Name,
                FilterChannelId = channel.Id,
            }));
            if (existingVideosWithoutChannel.Any())
            {
                await _meilisearchAccess.AddChannelId(existingVideosWithoutChannel.Select(z => z.Id), channel.Id);
            }
            request.Continuation = response.Continuation;
            if (string.IsNullOrEmpty(response.Continuation))
            {
                channel.ScrapedToOldest = true;
            }
            if (request.Continuation == null || (anyExistingVideosWithChannel && channel.ScrapedToOldest) || stoppingToken.IsCancellationRequested)
            {
                return;
            }
            var throttleTime = new Random().Next(1000, 2000);
            await Task.Delay(throttleTime);
        }
    }

    private VideoEntity TranslateToEntity(VideoObject videoObject)
    {
        var videoThumnails = videoObject.VideoThumbnails?.Select(MakeUrlRelative);
        var thumbnailsJson = System.Text.Json.JsonSerializer.Serialize(videoThumnails);
        return new VideoEntity
        {
            Title = videoObject.Title,
            UniqueId = videoObject.VideoId,
            Author = videoObject.Author,
            AuthorId = videoObject.AuthorId,
            AuthorUrl = videoObject.AuthorUrl,
            AuthorVerified = videoObject.AuthorVerified,
            ThumbnailsJson = thumbnailsJson,
            Description = videoObject.Description,
            ViewCount = videoObject.ViewCount,
            LengthSeconds = videoObject.LengthSeconds,
            EstimatedPublished = videoObject.Published,
            PremiereTimestamp = videoObject.PremiereTimestamp,
            LiveNow = videoObject.LiveNow,
            Premium = videoObject.Premium,
            IsUpcoming = videoObject.IsUpcoming
        };
    }

    /// <summary>
    /// Should be called prior to storing an image URL
    /// </summary>
    private VideoThumbnail MakeUrlRelative(VideoThumbnail videoThumbnail)
    {
        var urlPool = _invidiousUrlsAccess.GetAllInvidiousUrls();
        var match = urlPool.FirstOrDefault(url => videoThumbnail.Url.StartsWith(url));
        if (match != null)
        {
            videoThumbnail.Url = videoThumbnail.Url.Substring(match.Length);
        }
        return videoThumbnail;
    }
}