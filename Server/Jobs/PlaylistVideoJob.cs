
using Meilisearch;
using MyVidious.Access;
using MyVidious.Data;
using MyVidious.Models;
using MyVidious.Models.Invidious;
using MyVidious.Utilities;
using Quartz;
using System.Linq;

namespace MyVidious.Background;

public class PlaylistVideoJob : IJob
{
    private InvidiousAPIAccess _invidiousAPIAccess;
    private IServiceScopeFactory _serviceScopeFactory;
    private InvidiousUrlsAccess _invidiousUrlsAccess;
    private MeilisearchAccess _meilisearchAccess;
    private ISchedulerFactory _schedulerFactory;

    public PlaylistVideoJob(
        IServiceScopeFactory serviceScopeFactory,
        InvidiousAPIAccess invidiousAPIAccess,
        InvidiousUrlsAccess invidiousUrlsAccess,
        MeilisearchAccess meilisearchAccess,
        ISchedulerFactory schedulerFactory
        )
    {
        _invidiousAPIAccess = invidiousAPIAccess;
        _serviceScopeFactory = serviceScopeFactory;
        _invidiousUrlsAccess = invidiousUrlsAccess;
        _meilisearchAccess = meilisearchAccess;
        _schedulerFactory = schedulerFactory;
    }

    async Task IJob.Execute(IJobExecutionContext context)
    {
        await UpdateAllPlaylistVideos(context.CancellationToken);
    }

    private async Task UpdateAllPlaylistVideos(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var videoDbContext = scope.ServiceProvider.GetRequiredService<VideoDbContext>();
        var cutoff = DateTime.UtcNow.AddHours(-48);
        var playlists = videoDbContext.Playlists.Where(z => z.DateLastScraped == null || z.DateLastScraped < cutoff).Take(10).ToList();
        var sortedPlaylist = playlists.Where(z => z.DateLastScraped == null).Concat(playlists.Where(z => z.DateLastScraped != null));
        foreach (var playlist in sortedPlaylist)
        {
            await UpdatePlaylistVideos(videoDbContext, playlist);
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task UpdatePlaylistVideos(VideoDbContext videoDbContext, PlaylistEntity playlist)
    {

        Models.Invidious.PlaylistResponse response;
        try
        {
            response = await _invidiousAPIAccess.GetPlaylist(playlist.UniqueId);
        }
        catch (Exception)
        {
            Console.WriteLine($"Failed to load playlist {playlist.Title}");
            playlist.ScrapeFailureCount++;
            playlist.DateLastScraped = DateTime.UtcNow;
            videoDbContext.SaveChanges();
            return;
        }
        var responseVideos = response.Videos.Where(z => z.Title != "[Private video]" && z.Title != "[Deleted Video]").ToList();
        playlist.ScrapeFailureCount = 0;//we reset this each time there's a success
        var uniqueIds = responseVideos.Select(z => z.VideoId).ToList();
        var existingVideos = videoDbContext.Videos.Where(z => uniqueIds.Contains(z.UniqueId)).Select(z => new { z.UniqueId, z.Id }).ToList();
        var existingUniqueIds = existingVideos.Select(z => z.UniqueId).ToList();
        var existingVideoIds = existingVideos.Select(z => z.Id).ToList();
        var existingPlaylistVideos = videoDbContext.PlaylistVideos.Where(z => z.PlaylistId == playlist.Id).ToList();
        var videosToAdd = responseVideos.Where(z => !existingUniqueIds.Contains(z.VideoId)).Select(TranslateToEntity).ToList();
        videoDbContext.Videos.AddRange(videosToAdd);
        videoDbContext.SaveChanges();//this should assign a Id to each VideoEntity in videosToAdd

        await _meilisearchAccess.AddItems(videosToAdd.Select(z => new MeilisearchItem
        {
            VideoId = z.Id,
            FilterChannelId = null,
            FilterPlaylistIds = [playlist.Id],
            Name = z.Title,
            SecondName = z.Author,
        }));

        var preservedPlaylistVideos = existingPlaylistVideos.Where(z => existingVideoIds.Contains(z.VideoId)).ToList();
        var existingVideoIdsToAdd = existingVideoIds.Where(z => !preservedPlaylistVideos.Select(z => z.VideoId).Contains(z)).ToList();
        var playlistVideosToAdd = existingVideoIdsToAdd.Concat(videosToAdd.Select(z => z.Id)).Select(z => new PlaylistVideoEntity
        {
            VideoId = z,
            PlaylistId = playlist.Id
        }).ToList();
        var removedPlaylistVideos = existingPlaylistVideos.Except(preservedPlaylistVideos).ToList();
        var removedPlaylistVideoIds = removedPlaylistVideos.Select(z => z.VideoId).ToList();
        videoDbContext.PlaylistVideos.RemoveRange(removedPlaylistVideos);
        videoDbContext.PlaylistVideos.AddRange(playlistVideosToAdd);

        //before saving, we need to update the Index (playlist position) of each PlaylistVideo
        foreach (var playlistVideo in preservedPlaylistVideos.Concat(playlistVideosToAdd))
        {
            //The playlistVideo has a videoId, but we need a UniqueId to get the Index from responseVideos
            //the videoId is guarenteed to be found in either existingVideos or videosToAdd
            var uniqueId = existingVideos.FirstOrDefault(z => z.Id == playlistVideo.VideoId)?.UniqueId
                ?? videosToAdd.First(z => z.Id == playlistVideo.VideoId).UniqueId;
            playlistVideo.Index = responseVideos.First(z => z.VideoId == uniqueId).Index;
        }
        playlist.DateLastScraped = DateTime.UtcNow;
        videoDbContext.SaveChanges();
        
        if (existingVideoIdsToAdd.Any())
        {
            await _meilisearchAccess.AddPlaylistId(existingVideoIdsToAdd, playlist.Id);
        }
        if (removedPlaylistVideos.Any())
        {
            await _meilisearchAccess.RemovePlaylistId(removedPlaylistVideoIds, playlist.Id);
        }

        if (videosToAdd.Any())
        {
            //the video in PlaylistResponse.Videos doesn't contain all the information we want regarding a video
            //We need to individually load each video one-at-a-time, but we'll do that in a different job
            var newVideoIdsCSV = string.Join(",", videosToAdd.Select(z => z.Id));
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobDataMap = new JobDataMap(new Dictionary<string, string> { { VideoDetailsJob.JOB_DATA_KEY, newVideoIdsCSV } });
            await scheduler.TriggerJob(JobKey.Create(nameof(VideoDetailsJob)), jobDataMap);
        }

        var throttleTime = new Random().Next(1000, 2000);
        await Task.Delay(throttleTime);
    }

    private VideoEntity TranslateToEntity(PlaylistResponseVideo playlistVideo)
    {
        var videoThumnails = playlistVideo.VideoThumbnails?.Select(MakeUrlRelative);
        var thumbnailsJson = System.Text.Json.JsonSerializer.Serialize(videoThumnails);
        return new VideoEntity
        {
            Title = playlistVideo.Title,
            UniqueId = playlistVideo.VideoId,
            Author = playlistVideo.Author,
            AuthorId = playlistVideo.AuthorId,
            AuthorUrl = playlistVideo.AuthorUrl,
            AuthorVerified = false,
            ThumbnailsJson = thumbnailsJson,
            LengthSeconds = playlistVideo.LengthSeconds
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