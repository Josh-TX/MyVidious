
using Meilisearch;
using MyVidious.Access;
using MyVidious.Data;
using MyVidious.Models;

namespace MyVidious.Background; 

public class BackgroundRunner : BackgroundService
{
    private InvidiousAPIAccess _invidiousAPIAccess;
    private IServiceScopeFactory _serviceScopeFactory;

    public BackgroundRunner(IServiceScopeFactory serviceScopeFactory, InvidiousAPIAccess invidiousAPIAccess)
    {
        _invidiousAPIAccess = invidiousAPIAccess;
        _serviceScopeFactory = serviceScopeFactory;
    }

    private readonly PeriodicTimer _timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            do
            {
                await UpdateChannelVideos(stoppingToken);
            }
            while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested);
        } catch (Exception)
        {

        }
    }

    private async Task UpdateChannelVideos(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var videoDbContext = scope.ServiceProvider.GetRequiredService<VideoDbContext>();
        var incompleteChannels = videoDbContext.Channels.Where(z => z.DateLastScraped == null || !z.ScrapedToOldest).Take(10).ToList();
        var sortedChannels = incompleteChannels.Where(z => z.DateLastScraped == null).Concat(incompleteChannels.Where(z => z.DateLastScraped != null && z.ScrapeFailureCount < 3));
        foreach(var channel in sortedChannels)
        {
            await UpdateChannelVideos(stoppingToken, videoDbContext, channel);
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }

        var cutoff = DateTime.Now.AddHours(-48);
        var channelToUpdate = videoDbContext.Channels.Where(z => z.DateLastScraped < cutoff).ToList();
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
        var request = new Models.ChannelVideosRequest
        {
            Sort_by = "newest"
        };
        while(true)
        {
            Models.ChannelVideosResponse response;
            try
            {
                response = await _invidiousAPIAccess.GetChannelVideos(channel.UniqueId, request);
            }
            catch (Exception)
            {
                channel.ScrapeFailureCount++;
                channel.DateLastScraped = DateTime.Now;
                videoDbContext.SaveChanges();
                return;
            }
            var uniqueIds = response.Videos.Select(z => z.VideoId).ToList();
            var existingUniqueIds = videoDbContext.Videos.Where(z => uniqueIds.Contains(z.UniqueId)).Select(z => z.UniqueId).ToList();
            var videosToAdd = response.Videos.Where(z => !existingUniqueIds.Contains(z.VideoId)).Select(TranslateToEntity).ToList();
            foreach(var video in videosToAdd)
            {
                video.Channel = channel;
            }
            videoDbContext.AddRange(videosToAdd);
            if (string.IsNullOrEmpty(response.Continuation))
            {
                channel.ScrapedToOldest = true;
            }
            channel.DateLastScraped = DateTime.Now;
            videoDbContext.SaveChanges();

            request.Continuation = response.Continuation;
            if (request.Continuation == null || (existingUniqueIds.Any() && channel.ScrapedToOldest) || stoppingToken.IsCancellationRequested)
            {
                return;
            }
            await Task.Delay(5000);//throttle
        }
       
    }

    private async Task AddToMeilisearch(List<VideoEntity> videoEntities)
    {
        var meilisearchVideos = videoEntities.Select(z => new VideoMeilisearch
        {
            Id = z.Id,
            Title = z.Title,
            Description = z.Description.Substring(0, Math.Min(500, z.Description.Length)),
            ChannelHandle = z.Channel.Handle,
            ChannelName = z.Channel.Name,
        });
        MeilisearchClient client = new MeilisearchClient("http://localhost:7700", "aSampleMasterKey");
        var index = client.Index("videos");
        await index.UpdateSearchableAttributesAsync(new[] { "Title", "ChannelName", "ChannelHandle", "Description" });
        await index.AddDocumentsAsync(meilisearchVideos);
    }

    private VideoEntity TranslateToEntity(VideoObject videoObject)
    {
        var thumbnailsJson = System.Text.Json.JsonSerializer.Serialize(videoObject.VideoThumbnails);
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
            DescriptionHtml = videoObject.DescriptionHtml,
            ViewCount = videoObject.ViewCount,
            ViewCountText = videoObject.ViewCountText,
            LengthSeconds = videoObject.LengthSeconds,
            Published = videoObject.Published,
            PublishedText = videoObject.PublishedText,
            PremiereTimestamp = videoObject.PremiereTimestamp,
            LiveNow = videoObject.LiveNow,
            Premium = videoObject.Premium,
            IsUpcoming = videoObject.IsUpcoming
        };
    }
}