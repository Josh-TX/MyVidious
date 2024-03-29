namespace MyVidious.Models.Invidious;

/// <summary>
/// Should match the response of /api/v1/channels/{channelId}
/// </summary>
public class ChannelResponse
{
    public required string Author { get; set; }
    public required string AuthorId { get; set; }
    public string? AuthorUrl { get; set; }
    public bool AuthorVerified { get; set; }
    public IEnumerable<ImageObject>? AuthorBanners { get; set; }
    public IEnumerable<AuthorThumbnail>? AuthorThumbnails { get; set; }

    public int SubCount { get; set; }
    public int TotalViews { get; set; }
    public int Joined { get; set; }

    public bool AutoGenerated { get; set; }
    public bool IsFamilyFriendly { get; set; }

    public string? Description { get; set; }
    public string? DescriptionHtml { get; set; }
    public IEnumerable<string>? AllowedRegions { get; set; }

    public IEnumerable<string>? Tabs { get; set; }
    public IEnumerable<VideoObject>? LatestVideos { get; set; }
    public IEnumerable<ChannelObject>? RelatedChannels { get; set; }

}