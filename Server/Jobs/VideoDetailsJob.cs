
using Meilisearch;
using MyVidious.Access;
using MyVidious.Data;
using MyVidious.Models;
using MyVidious.Models.Invidious;
using MyVidious.Utilities;
using Quartz;
using System.Linq;

namespace MyVidious.Background;

public class VideoDetailsJob : IJob
{
    public const string JOB_DATA_KEY = "videoIds";

    private InvidiousAPIAccess _invidiousAPIAccess;
    private IServiceScopeFactory _serviceScopeFactory;
    private InvidiousUrlsAccess _invidiousUrlsAccess;
    private MeilisearchAccess _meilisearchAccess;

    public VideoDetailsJob(
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
        if (context.Trigger.JobDataMap.TryGetString("videoIds", out var videoIdCSV))
        {
            var videoIds = videoIdCSV!.Split(',').Select(int.Parse).ToList();
            await UpdateVideos(context.CancellationToken, videoIds);
        } else
        {

        }
    }

    private async Task UpdateVideos(CancellationToken stoppingToken, IEnumerable<int> videoIds)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var videoDbContext = scope.ServiceProvider.GetRequiredService<VideoDbContext>();
        var cutoff = DateTime.UtcNow.AddHours(-48);
        var videos = videoDbContext.Videos.Where(z => videoIds.Contains(z.Id)).ToList();
        foreach (var video in videos)
        {
            await UpdateVideo(videoDbContext, video);
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task UpdateVideo(VideoDbContext videoDbContext, VideoEntity video)
    {

        Models.Invidious.VideoResponse response;
        try
        {
            response = await _invidiousAPIAccess.GetVideo(video.UniqueId);
        }
        catch (Exception)
        {
            Console.WriteLine($"Failed to scrape video details for video {video.Title}");
            return;
        }
        var videoThumnails = response.VideoThumbnails?.Select(MakeUrlRelative);
        var thumbnailsJson = System.Text.Json.JsonSerializer.Serialize(videoThumnails);
        video.Title = response.Title;
        video.AuthorUrl = response.AuthorUrl;
        video.AuthorVerified = response.AuthorVerified;
        video.ThumbnailsJson = thumbnailsJson;
        video.Description = response.Description;
        video.ViewCount = response.ViewCount;
        video.LengthSeconds = response.LengthSeconds;
        video.EstimatedPublished = response.Published; //needed because the algorithm queries use estimatedPublished
        video.ActualPublished = response.Published;
        video.PremiereTimestamp = response.PremiereTimestamp;
        video.LiveNow = response.LiveNow;
        video.Premium = response.Premium;
        video.IsUpcoming = response.IsUpcoming;
        videoDbContext.SaveChanges();
        
        var throttleTime = new Random().Next(1000, 2000);
        await Task.Delay(throttleTime);
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