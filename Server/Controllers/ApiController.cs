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
        var channelIds = _algorithmAccess.GetChannelIds(username, algorithm);
        if (string.IsNullOrEmpty(request.Q))
        {
            return Ok(Enumerable.Empty<object>());
        }
        if (request.Type == "channel")
        {
            var foundChannelIds = await _meilisearchAccess.SearchChannelIds(request.Q, request.Page, channelIds, true);
            var channels = _videoDbContext.Channels.Where(z => foundChannelIds.Contains(z.Id));
            var searchResults = channels.Select(TranslateToSearchResponse);
            return Ok(searchResults.Select(z => (object)z));
        }
        else if (request.Type == "video")
        {
            var videoIds = await _meilisearchAccess.SearchVideoIds(request.Q, request.Page, channelIds);
            var videos = _videoDbContext.Videos.Where(z => videoIds.Contains(z.Id));
            var searchResults = videos.Select(TranslateToSearchResponse);
            return Ok(searchResults.Select(z => (object)z));
        }
        else if (request.Type == "playlist" || request.Type == "movie" || request.Type == "show")
        {
            return Ok(Enumerable.Empty<object>());
        }
        else 
        {
            var videoIds = await _meilisearchAccess.SearchVideoIds(request.Q, request.Page, channelIds);
            var videos = _videoDbContext.Videos.Where(z => videoIds.Contains(z.Id));
            var searchResults_video = videos.Select(TranslateToSearchResponse);
            var foundChannelIds = await _meilisearchAccess.SearchChannelIds(request.Q, request.Page, channelIds, true);
            var channels = _videoDbContext.Channels.Where(z => foundChannelIds.Contains(z.Id));
            var searchResults_channel = channels.Select(TranslateToSearchResponse);
            var merged = MergeVideosAndChannels(searchResults_channel, searchResults_video);
            return Ok(merged.Select(z => (object)z));
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

    private IEnumerable<SearchResponseBase> MergeVideosAndChannels(IEnumerable<SearchResponse_Channel> channels, IEnumerable<SearchResponse_Video> videos)
    {
        var baseVideos = videos.OfType<SearchResponseBase>();
        var baseChannels = channels.OfType<SearchResponseBase>();
        //order is VV C VVV C VVVVV C VVVVVVVVVVVVVVVVVVVVVVVVVV
        var result = baseVideos.Take(2)
            .Concat(baseChannels.Take(1))
            .Concat(baseVideos.Skip(2).Take(3))
            .Concat(baseChannels.Skip(1).Take(1))
            .Concat(baseVideos.Skip(5).Take(5))
            .Concat(baseChannels.Skip(2).Take(1))
            .Concat(baseVideos.Skip(10))
            .ToList();
        return result;
    }
}
