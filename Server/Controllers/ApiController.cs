using Microsoft.AspNetCore.Mvc;
using MyVidious.Access;
using MyVidious.Data;
using Microsoft.EntityFrameworkCore;
using Meilisearch;
using MyVidious.Models.Invidious;
using MyVidious.Utilities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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
        var recommendedVideos = _algorithmAccess.GetRandomAlgorithmVideos(username, algorithm, videoResponse.RecommendedVideos?.Count() ?? 19);
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
            var items = await _meilisearchAccess.SearchItems(request.Q, request.Page, channelAndPlaylistIds);
            var channelIds = items.Where(z => z.ChannelId.HasValue).Select(z => z.ChannelId!.Value);
            var channels = _videoDbContext.Channels.Where(z => channelIds.Contains(z.Id));
            var searchResults = channels.Select(TranslateToSearchResponse);
            return Ok(searchResults.Select(z => (object)z));
        }
        else if (request.Type == "video")
        {
            var items = await _meilisearchAccess.SearchItems(request.Q, request.Page, channelAndPlaylistIds);
            var videoIds = items.Where(z => z.VideoId.HasValue).Select(z => z.VideoId!.Value);
            var videos = _videoDbContext.Videos.Where(z => videoIds.Contains(z.Id));
            var searchResults = videos.Select(TranslateToSearchResponse);
            return Ok(searchResults.Select(z => (object)z));
        }
        else if (request.Type == "playlist")
        {
            var items = await _meilisearchAccess.SearchItems(request.Q, request.Page, channelAndPlaylistIds, MeilisearchType.Playlist);
            var playlistIds = items.Where(z => z.PlaylistId.HasValue).Select(z => z.PlaylistId!.Value);
            var playlists = _videoDbContext.Playlists.Where(z => playlistIds.Contains(z.Id));
            var searchResults = playlists.Select(TranslateToSearchResponse);
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
            var videos = _videoDbContext.Videos.Where(z => videoIds.Contains(z.Id)).ToList();

            var channelIds = items.Where(z => z.ChannelId.HasValue).Select(z => z.ChannelId!.Value);
            var channels = _videoDbContext.Channels.Where(z => channelIds.Contains(z.Id)).ToList();

            var playlistIds = items.Where(z => z.PlaylistId.HasValue).Select(z => z.PlaylistId!.Value);
            var playlists = _videoDbContext.Playlists.Where(z => playlistIds.Contains(z.Id)).ToList();

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
                    return foundPlaylist == null ? null : (SearchResponseBase?)TranslateToSearchResponse(foundPlaylist);
                }
                return (SearchResponseBase?)null;
            });
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

    private SearchResponse_Video TranslateToSearchResponse(VideoEntity video)
    {
        var thumbnails = video.ThumbnailsJson != null
            ? System.Text.Json.JsonSerializer.Deserialize<IEnumerable<VideoThumbnail>>(video.ThumbnailsJson)!.ToList()
            : Enumerable.Empty<VideoThumbnail>();
        thumbnails = thumbnails.Select(_imageUrlUtility.FixImageUrl).ToList();
        var published = video.ActualPublished ?? video.EstimatedPublished ?? DateTime.Now.ToFileTimeUtc();
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

    private SearchResponse_Playlist TranslateToSearchResponse(PlaylistEntity playlist)
    {
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
            VideoCount = playlist.VideoCount
        };
    }
}
