
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
        System.Diagnostics.Debug.WriteLine("VideoFetchJob: " + DateTime.Now);
        await UpdateChannelVideos(context.CancellationToken);
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

        var cutoff = DateTime.UtcNow.AddHours(-48);
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
                channel.ScrapeFailureCount++;
                channel.DateLastScraped = DateTime.UtcNow;
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
            channel.DateLastScraped = DateTime.UtcNow;
            videoDbContext.SaveChanges();
            await _meilisearchAccess.AddVideos(videosToAdd.Select(z => new VideoMeilisearch
            {
                Id = z.Id,
                Title = z.Title,
                ChannelName = z.Channel.Name,
                ChannelId = z.ChannelId
            }));
            request.Continuation = response.Continuation;
            if (request.Continuation == null || (existingUniqueIds.Any() && channel.ScrapedToOldest) || stoppingToken.IsCancellationRequested)
            {
                return;
            }
            await Task.Delay(5000);//throttle
        }
    }

    private VideoEntity TranslateToEntity(VideoObject videoObject)
    {
        var videoThumnails = videoObject.VideoThumbnails.Select(MakeUrlRelative);
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