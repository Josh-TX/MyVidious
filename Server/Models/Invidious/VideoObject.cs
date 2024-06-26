using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyVidious.Models.Invidious;

/// <summary>
/// Should match the VideoObject in the documentation
/// </summary>
public class VideoObject
{
    public required string Type { get; set; }
    public required string Title { get; set; }
    public required string VideoId { get; set; }

    public required string Author { get; set; }
    public required string AuthorId { get; set; }
    public string? AuthorUrl { get; set; }
    public bool AuthorVerified { get; set; }

    public List<VideoThumbnail>? VideoThumbnails { get; set; }
    public string? Description { get; set; }
    public string? DescriptionHtml { get; set; }

    public long ViewCount { get; set; }
    public string? ViewCountText { get; set; }
    public int LengthSeconds { get; set; }

    public long Published { get; set; }
    public string? PublishedText { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? PremiereTimestamp { get; set; }//listed in documentation, but APIs don't return it
    public bool LiveNow { get; set; }
    public bool Premium { get; set; }
    public bool IsUpcoming { get; set; }
}