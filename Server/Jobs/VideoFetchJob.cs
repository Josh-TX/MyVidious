
using Meilisearch;
using MyVidious.Access;
using MyVidious.Data;
using MyVidious.Models;
using MyVidious.Models.Invidious;
using MyVidious.Utilities;
using Quartz;

namespace MyVidious.Background; 

public class VideoFetchJob : IJob
{
    private InvidiousAPIAccess _invidiousAPIAccess;
    private IServiceScopeFactory _serviceScopeFactory;
    private AppSettings _appSettings;
    private InvidiousUrlsAccess _invidiousUrlsAccess;
    private MeilisearchAccess _meilisearchAccess;

    public VideoFetchJob(
        IServiceScopeFactory serviceScopeFactory, 
        InvidiousAPIAccess invidiousAPIAccess, 
        AppSettings appSettings, 
        InvidiousUrlsAccess invidiousUrlsAccess,
        MeilisearchAccess meilisearchAccess)
    {
        _invidiousAPIAccess = invidiousAPIAccess;
        _serviceScopeFactory = serviceScopeFactory;
        _appSettings = appSettings;
        _invidiousUrlsAccess = invidiousUrlsAccess;
        _meilisearchAccess = meilisearchAccess;
    }

    async Task IJob.Execute(IJobExecutionContext context)
    {
        await UpdateChannelVideos(context.CancellationToken);
    }

    private async Task UpdateChannelVideos(CancellationToken stoppingToken)
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
        int count = 0;
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
            var existingUniqueIds = videoDbContext.Videos.Where(z => uniqueIds.Contains(z.UniqueId)).Select(z => z.UniqueId).ToList();
            var videosToAdd = response.Videos.Where(z => !existingUniqueIds.Contains(z.VideoId)).Select(TranslateToEntity).ToList();
            foreach(var video in videosToAdd)
            {
                count++;
                video.Channel = channel;
            }
            videoDbContext.AddRange(videosToAdd);
            if (!channel.ScrapedToOldest)
            {
                channel.VideoCount = count;
            } else
            {
                channel.VideoCount = channel.VideoCount + videosToAdd.Count;
            }
            if (string.IsNullOrEmpty(response.Continuation))
            {
                channel.ScrapedToOldest = true;
            }
            channel.DateLastScraped = DateTime.UtcNow;
            Console.WriteLine($"scraped {videosToAdd.Count} videos for channel {channel.Name}");
            videoDbContext.SaveChanges();
            await _meilisearchAccess.AddVideos(videosToAdd.Select(z => new VideoMeilisearch
            {
                Id = z.Id,
                Title = z.Title,
                ChannelName = channel.Name,
                ChannelId = z.ChannelId
            }));
            request.Continuation = response.Continuation;
            if (request.Continuation == null || (existingUniqueIds.Any() && channel.ScrapedToOldest) || stoppingToken.IsCancellationRequested)
            {
                return;
            }
            var throttleTime = new Random().Next(1000, 20000);
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
            Published = videoObject.Published,
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