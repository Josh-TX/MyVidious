namespace MyVidious.Models.Invidious;

/// <summary>
/// Should match the response of /api/v1/videos/{videoId}
/// </summary>
public class VideoResponse
{
    public required string Title { get; set; }
    public required string VideoId { get; set; }
    public IEnumerable<VideoThumbnail>? VideoThumbnails { get; set; }
    public string? Description { get; set; }
    public string? DescriptionHtml { get; set; }
    public long Published { get; set; }
    public string? PublishedText { get; set; }
    public IEnumerable<string>? Keywords { get; set; }
    public long ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int DislikeCount { get; set; }
    public bool Paid { get; set; }
    public bool Premium { get; set; }
    public bool IsFamilyFriendly { get; set; }
    public IEnumerable<string>? AllowedRegions { get; set; }
    public string? Genre { get; set; }
    public string? GenreUrl { get; set; }
    public required string Author { get; set; }
    public required string AuthorId { get; set; }
    public string? AuthorUrl { get; set; }
    public IEnumerable<AuthorThumbnail>? AuthorThumbnails { get; set; }
    public string? SubCountText { get; set; }
    public int LengthSeconds { get; set; }
    public bool AllowRatings { get; set; }
    public float Rating { get; set; }
    public bool IsListed { get; set; }
    public bool LiveNow { get; set; }
    public bool IsUpcoming { get; set; }

    //PremiereTimestamp and HlsUrl don't seem to exist on the invidious API, but the documentation says they do
    public long PremiereTimestamp { get; set; }
    public string? HlsUrl { get; set; }
    public IEnumerable<object>? AdaptiveFormats { get; set; }
    public IEnumerable<object>? FormatStreams { get; set; }
    public IEnumerable<object>? Captions { get; set; }
    public IEnumerable<RecommendedVideo>? RecommendedVideos { get; set; }

    //as of Feb 2024, these fields are part of the API, but not listed in the documentation
    public string? Type {get;set;}
    public string? DashUrl {get;set;}
    public bool AuthorVerified {get;set;}
}


// This model seems to only be used by VideoResponse
public class RecommendedVideo
{
    public required string VideoId { get; set; }
    public required string Title { get; set; }
    public IEnumerable<VideoThumbnail>? VideoThumbnails { get; set; }
    public required string Author { get; set; }
    public int LengthSeconds { get; set; }
    public string? ViewCountText { get; set; }

    //as of Feb 2024, these fields are part of the API, but not listed in the documentation
    public string? AuthorUrl { get; set; }
    public required string AuthorId { get; set; }
    public long ViewCount { get; set; }
}