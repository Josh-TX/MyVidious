using Microsoft.AspNetCore.Mvc;
using MyVidious.Access;
using MyVidious.Data;
using Microsoft.EntityFrameworkCore;
using MyVidious.Models.Invidious;
using MyVidious.Utilities;

namespace MyVidious.Controllers;

[Route("")]
[ApiController]
public class ApiController : Controller
{
    private InvidiousAPIAccess _invidiousAPIAccess;
    private VideoDbContext _videoDbContext;
    private AlgorithmAccess _algorithmAccess;
    private ImageUrlUtility _imageUrlUtility;
    private AppSettings _appSettings;
    private MeilisearchAccess _meilisearchAccess;

    public ApiController(
        InvidiousAPIAccess invidiousAPIAccess,
        VideoDbContext videoDbContext,
        AlgorithmAccess algorithmAccess,
        ImageUrlUtility imageUrlUtility,
        AppSettings appSettings,
        MeilisearchAccess meilisearchAccess
        )
    {
        _invidiousAPIAccess = invidiousAPIAccess;
        _videoDbContext = videoDbContext;
        _algorithmAccess = algorithmAccess;
        _imageUrlUtility = imageUrlUtility;
        _appSettings = appSettings;
        _meilisearchAccess = meilisearchAccess;
    }

    [Route("{username}/{algorithm}/api/v1/videos/{videoId}")]
    [HttpGet]
    public async Task<VideoResponse> GetVideo([FromRoute] string username, [FromRoute] string algorithm, [FromRoute] string videoId)
    {
        var videoResponse = await _invidiousAPIAccess.GetVideo(videoId);
        videoResponse.VideoThumbnails = videoResponse.VideoThumbnails?.Select(_imageUrlUtility.FixImageUrl).ToList();
        var recommendedVideos = _algorithmAccess.GetRandomAlgorithmVideos(username, algorithm, videoResponse.RecommendedVideos?.Count() ?? 19).ToList();
        AddCustomRecommendation(username, algorithm, recommendedVideos, videoResponse.RecommendedVideos, videoResponse); //mutates recommendedVideos
        videoResponse.RecommendedVideos = recommendedVideos;
        return videoResponse;
    }

    [Route("{username}/{algorithm}/api/v1/search")]
    [HttpGet]
    public async Task<IActionResult> GetSearchResults([FromRoute] string username, [FromRoute] string algorithm, [FromQuery] SearchRequest request)
    {
        var channelAndPlaylistIds = _algorithmAccess.GetChannelAndPlaylistIds(username, algorithm);
        if (string.IsNullOrEmpty(request.Q))
        {
            return Ok(Enumerable.Empty<object>());
        }
        if (request.Type == "channel")
        {
            var items = await _meilisearchAccess.SearchItems(request.Q, request.Page, channelAndPlaylistIds, MeilisearchType.Channel);
            var channelIds = items.Where(z => z.ChannelId.HasValue).Select(z => z.ChannelId!.Value);
            if (!channelIds.Any())
            {
                return Ok(Enumerable.Empty<object>());
            }
            var channels = _videoDbContext.Channels.Where(z => channelIds.Contains(z.Id)).ToList();
            var searchResults = channels.Select(TranslateToSearchResponse).ToList();
            return Ok(searchResults.Select(z => (object)z));
        }
        else if (request.Type == "video")
        {
            var items = await _meilisearchAccess.SearchItems(request.Q, request.Page, channelAndPlaylistIds, MeilisearchType.Video);
            var videoIds = items.Where(z => z.VideoId.HasValue).Select(z => z.VideoId!.Value);
            if (!videoIds.Any())
            {
                return Ok(Enumerable.Empty<object>());
            }
            var videos = _videoDbContext.Videos.Where(z => videoIds.Contains(z.Id)).ToList();
            var searchResults = videos.Select(TranslateToSearchResponse).ToList();
            return Ok(searchResults.Select(z => (object)z));
        }
        else if (request.Type == "playlist")
        {
            var items = await _meilisearchAccess.SearchItems(request.Q, request.Page, channelAndPlaylistIds, MeilisearchType.Playlist);
            var playlistIds = items.Where(z => z.PlaylistId.HasValue).Select(z => z.PlaylistId!.Value);
            if (!playlistIds.Any())
            {
                return Ok(Enumerable.Empty<object>());
            }
            var playlists = _videoDbContext.Playlists.Where(z => playlistIds.Contains(z.Id)).ToList();
            var allTempPlaylistVideos = GetTempPlaylistVideos(playlistIds);
            var searchResults = playlists.Select(z => TranslateToSearchResponse(z, allTempPlaylistVideos)).ToList();
            return Ok(searchResults.Select(z => (object)z));
        }
        else if ( request.Type == "movie" || request.Type == "show")
        {
            return Ok(Enumerable.Empty<object>());
        }
        else //all
        {
            var items = await _meilisearchAccess.SearchItems(request.Q, request.Page, channelAndPlaylistIds);

            var videoIds = items.Where(z => z.VideoId.HasValue).Select(z => z.VideoId!.Value);
            var videos = videoIds.Any() 
                ? _videoDbContext.Videos.Where(z => videoIds.Contains(z.Id)).ToList()
                : new List<VideoEntity>();

            var channelIds = items.Where(z => z.ChannelId.HasValue).Select(z => z.ChannelId!.Value);
            var channels = channelIds.Any()
                ? _videoDbContext.Channels.Where(z => channelIds.Contains(z.Id)).ToList()
                : new List<ChannelEntity>();

            var playlistIds = items.Where(z => z.PlaylistId.HasValue).Select(z => z.PlaylistId!.Value);
            var playlists = playlistIds.Any()
                ? _videoDbContext.Playlists.Where(z => playlistIds.Contains(z.Id)).ToList()
                : new List<PlaylistEntity>();

            var allTempPlaylistVideos = GetTempPlaylistVideos(playlistIds);
            var translatedItems = items.Select(item =>
            {
                if (item.VideoId.HasValue)
                {
                    var foundVideo = videos.FirstOrDefault(z => z.Id == item.VideoId.Value);
                    return foundVideo == null ? null : (SearchResponseBase?)TranslateToSearchResponse(foundVideo);
                }
                if (item.ChannelId.HasValue)
                {
                    var foundChannel = channels.First(z => z.Id == item.ChannelId.Value);
                    return foundChannel == null ? null : (SearchResponseBase?)TranslateToSearchResponse(foundChannel);
                }
                if (item.PlaylistId.HasValue)
                {
                    var foundPlaylist = playlists.First(z => z.Id == item.PlaylistId.Value);
                    return foundPlaylist == null ? null : (SearchResponseBase?)TranslateToSearchResponse(foundPlaylist, allTempPlaylistVideos);
                }
                return (SearchResponseBase?)null;
            }).ToList();
            return Ok(translatedItems.Where(z => z != null).Select(z => (object)z!));
        }
    }

    [Route("{username}/{algorithm}/api/v1/channels/{channelId}")]
    [HttpGet]
    public async Task<ChannelResponse> GetChannel([FromRoute] string channelId)
    {
        return await _invidiousAPIAccess.GetChannel(channelId);
    }

    [Route("{username}/{algorithm}/api/v1/trending")]
    [HttpGet]
    public IEnumerable<VideoObject> GetTrending([FromRoute] string username, [FromRoute] string algorithm)
    {
        return _algorithmAccess.GetTrendingAlgorithmVideos(username, algorithm, 60);
    }

    [Route("{username}/{algorithm}/api/v1/popular")]
    [HttpGet]
    public IEnumerable<PopularVideo> GetPopular([FromRoute] string username, [FromRoute] string algorithm)
    {
        return _algorithmAccess.GetPopularAlgorithmVideos(username, algorithm, 60);
    }

    [Route("{username}/{algorithm}/api/v1/channels/{channelId}/videos")]
    [HttpGet]
    public async Task<ChannelVideosResponse> GetChannelVideos([FromRoute] string channelId, [FromQuery] ChannelVideosRequest request)
    {
        return await _invidiousAPIAccess.GetChannelVideos(channelId, request);
    }

    private IEnumerable<TempPlaylistVideo> GetTempPlaylistVideos(IEnumerable<int> playlistIds)
    {
        if (!playlistIds.Any())
        {
            return Enumerable.Empty<TempPlaylistVideo>();
        }
        return _videoDbContext.Videos.Join(_videoDbContext.PlaylistVideos, v => v.Id, pv => pv.VideoId, (v, pv) => new TempPlaylistVideo
        {
            PlaylistId = pv.PlaylistId,
            Title = v.Title,
            UniqueId = v.UniqueId,
            LengthSeconds = v.LengthSeconds,
            ThumbnailsJson = v.ThumbnailsJson,
        }).Where(z => playlistIds.Contains(z.PlaylistId)).ToList();
    }

    private SearchResponse_Video TranslateToSearchResponse(VideoEntity video)
    {
        var thumbnails = video.ThumbnailsJson != null
            ? System.Text.Json.JsonSerializer.Deserialize<IEnumerable<VideoThumbnail>>(video.ThumbnailsJson)!.ToList()
            : Enumerable.Empty<VideoThumbnail>();
        thumbnails = thumbnails.Select(_imageUrlUtility.FixImageUrl).ToList();
        var published = video.ActualPublished ?? video.EstimatedPublished ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return new SearchResponse_Video
        {
            Type = "video",

            Title = video.Title,
            VideoId = video.UniqueId,

            Author = video.Author,
            AuthorId = video.AuthorId,
            AuthorUrl = video.AuthorUrl,
            AuthorVerified = video.AuthorVerified,

            VideoThumbnails = thumbnails,
            Description = video.Description,
            DescriptionHtml = System.Web.HttpUtility.HtmlEncode(video.Description),

            ViewCount = video.ViewCount,
            ViewCountText = Helpers.FormatViews(video.ViewCount),
            LengthSeconds = video.LengthSeconds,

            Published = published,
            PublishedText = Helpers.GetPublishedText(published),

            LiveNow = video.LiveNow,
            Premium = video.Premium,
            IsUpcoming = video.IsUpcoming
        };
    }

    private SearchResponse_Channel TranslateToSearchResponse(ChannelEntity channel)
    {
        var thumbnails = channel.ThumbnailsJson != null 
            ? System.Text.Json.JsonSerializer.Deserialize<IEnumerable<AuthorThumbnail>>(channel.ThumbnailsJson)!.ToList()
            : Enumerable.Empty<AuthorThumbnail>();
        thumbnails = thumbnails.Select(_imageUrlUtility.FixImageUrl).ToList();
        return new SearchResponse_Channel
        {
            Type = "channel",
            ChannelHandle = channel.Handle,
            Description = channel.Description,
            DescriptionHtml = System.Web.HttpUtility.HtmlEncode(channel.Description),

            Author = channel.Name,
            AuthorId = channel.UniqueId,
            AuthorUrl = channel.AuthorUrl,
            AuthorVerified = channel.AuthorVerified,

            AuthorThumbnails = thumbnails,

            AutoGenerated = channel.AutoGenerated,
            SubCount = channel.SubCount,
            VideoCount = channel.VideoCount
        };
    }

    private SearchResponse_Playlist TranslateToSearchResponse(PlaylistEntity playlist, IEnumerable<TempPlaylistVideo> allTempPlaylistVideos)
    {
        var matchingPlaylistVideos = allTempPlaylistVideos.Where(z => z.PlaylistId == playlist.Id);
        return new SearchResponse_Playlist
        {
            Type = "playlist",
            Title = playlist.Title,
            PlaylistId = playlist.UniqueId,
            PlaylistThumbnail = playlist.PlaylistThumbnail,
            Author = playlist.Author,
            AuthorId = playlist.UniqueId,
            AuthorUrl = playlist.AuthorUrl,
            AuthorVerified = false,
            VideoCount = playlist.VideoCount,
            Videos = matchingPlaylistVideos.Select(z =>
            {
                var thumbnails = z.ThumbnailsJson != null
                    ? System.Text.Json.JsonSerializer.Deserialize<IEnumerable<VideoThumbnail>>(z.ThumbnailsJson)!.ToList()
                    : Enumerable.Empty<VideoThumbnail>();
                thumbnails = thumbnails.Select(_imageUrlUtility.FixImageUrl).ToList();
                return new PlaylistVideo
                {
                    VideoId = z.UniqueId,
                    VideoThumbnails = thumbnails,
                    Title = z.Title,
                    LengthSeconds = z.LengthSeconds
                };
            }).ToList()
        };
    }

    /// <summary>
    // Progpogates a youtube suggestion for the matching channel. If It's for a playlist, try to get the next playlist item
    /// </summary>
    private void AddCustomRecommendation(
        string username,
        string algorithmName,
        List<RecommendedVideo> recommendedVideos, 
        IEnumerable<RecommendedVideo>? originalRecommendedVideos, 
        VideoResponse videoResponse)
    {
        var videoEntity = _videoDbContext.Videos.FirstOrDefault(z => z.UniqueId == videoResponse.VideoId);
        if (videoEntity != null)
        {
            Models.ChannelAndPlaylistIds channelAndPlaylistIds = _algorithmAccess.GetChannelAndPlaylistIds(username, algorithmName);
            //for performance reasons, check if the video is a part of the algorithm's channel
            if (videoEntity.ChannelId.HasValue && channelAndPlaylistIds.ChannelIds.Contains(videoEntity.ChannelId.Value))
            {
                var recommendedToAdd = originalRecommendedVideos?.FirstOrDefault(z => z.AuthorId == videoResponse.AuthorId);
                if (recommendedToAdd != null)
                {
                    recommendedVideos.Insert(new Random().Next(0, 3), recommendedToAdd);
                }
            }
            else
            {
                var currentPlaylistVideo = _videoDbContext.PlaylistVideos.FirstOrDefault(z => z.VideoId == videoEntity.Id);
                if (currentPlaylistVideo != null)
                {
                    var nextPlaylistVideo = _videoDbContext.PlaylistVideos
                        .Include(z => z.Video)
                        .FirstOrDefault(z => z.PlaylistId == currentPlaylistVideo.PlaylistId && z.Index == (currentPlaylistVideo.Index + 1))
                        ?.Video;
                    if (nextPlaylistVideo != null)
                    {
                        recommendedVideos.Insert(0, _algorithmAccess.TranslateToRecommended(nextPlaylistVideo));
                    }
                }
            }
        }
    }

    private class TempPlaylistVideo
    {
        public int PlaylistId { get; set; }
        public int LengthSeconds { get; set; }
        public required string Title { get; set; }
        public required string UniqueId { get; set; }
        public string? ThumbnailsJson { get; set; }
    }
}
