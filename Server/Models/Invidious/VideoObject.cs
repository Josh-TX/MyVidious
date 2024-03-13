using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyVidious.Models.Invidious;

/// <summary>
/// Should match the VideoObject in the documentation
/// </summary>
public class VideoObject
{
    public string Type { get; set; }
    public string Title { get; set; }
    public string VideoId { get; set; }

    public string Author { get; set; }
    public string AuthorId { get; set; }
    public string AuthorUrl { get; set; }
    public bool AuthorVerified { get; set; }

    public IEnumerable<VideoThumbnail> VideoThumbnails { get; set; }
    public string Description { get; set; }
    public string DescriptionHtml { get; set; }

    public long ViewCount { get; set; }
    public string ViewCountText { get; set; }
    public int LengthSeconds { get; set; }

    public long Published { get; set; }
    public string PublishedText { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PremiereTimestamp { get; set; }//listed in documentation, but APIs don't return it
    public bool LiveNow { get; set; }
    public bool Premium { get; set; }
    public bool IsUpcoming { get; set; }
}