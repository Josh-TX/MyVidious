using Microsoft.AspNetCore.Mvc;
using MyVidious.Access;
using MyVidious.Data;
using Microsoft.EntityFrameworkCore;
using Meilisearch;
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
        videoResponse.VideoThumbnails = videoResponse.VideoThumbnails.Select(_imageUrlUtility.FixImageUrl).ToList();
        var recommendedVideos = _algorithmAccess.GetRandomAlgorithmVideos(username, algorithm, videoResponse.RecommendedVideos.Count());
        videoResponse.RecommendedVideos = recommendedVideos;
        return videoResponse;
    }

    [Route("{username}/{algorithm}/api/v1/search")]
    [HttpGet]
    public async Task<IActionResult> GetSearchResults([FromRoute] string username, [FromRoute] string algorithm, [FromQuery] SearchRequest request)
    {
        var channelIds = _algorithmAccess.GetChannelIds(username, algorithm);
        var videoIds = await _meilisearchAccess.SearchVideoIds(request.Q, request.Page, channelIds);
        var videos = _videoDbContext.Videos.Where(z => videoIds.Contains(z.Id));
        var searchResultsBase = videos.Select(TranslateToSearchResponse);

        //MeilisearchClient client = new MeilisearchClient(_appSettings.MeilisearchUrl, "masterKey");
        //var index = client.Index("videos");
        //var searchable = await index.SearchAsync<VideoMeilisearch>(request.Q, new SearchQuery
        //{
        //    HitsPerPage = 20,
        //    Page = request.Page.HasValue ? request.Page.Value : 1
        //});
        //var videoIds = searchable.Hits.Select(z => z.Id).ToList();
        //var videos = _videoDbContext.Videos.Where(z => videoIds.Contains(z.Id));
        //var searchResultsBase = videos.Select(TranslateToSearchResponse);


        //var searchResultsBase = await _invidiousAPIAccess.Search(request)!;

        //System.Text.Json will only serialize properties on the base type.
        //but if we cast them to an object type, it'll serialize all properties on the underlying type
        return Ok(searchResultsBase.Select(z => (object)z));
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
        var thumbnails = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<VideoThumbnail>>(video.ThumbnailsJson)!.ToList();
        thumbnails = thumbnails.Select(_imageUrlUtility.FixImageUrl).ToList();
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
            DescriptionHtml = video.DescriptionHtml,

            ViewCount = video.ViewCount,
            ViewCountText = video.ViewCountText,
            LengthSeconds = video.LengthSeconds,

            Published = video.Published,
            PublishedText = video.PublishedText,

            LiveNow = video.LiveNow,
            Premium = video.Premium,
            IsUpcoming = video.IsUpcoming
        };
    }
}
