using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyVidious.Models.Invidious;

public class PopularVideo
{
    /// <summary>
    /// No idea why this is called "shortVideo"... you can have videos over an hour long in the "Popular" section
    /// </summary>
    public required string Type { get; set; }
    public required string Title { get; set; }


    public required string VideoId { get; set; }
    public List<VideoThumbnail>? VideoThumbnails { get; set; }


    public int LengthSeconds { get; set; }
    public long ViewCount { get; set; }

    public required string Author { get; set; }
    public required string AuthorId { get; set; }
    public string? AuthorUrl { get; set; }


    public long Published { get; set; }
    public string? PublishedText { get; set; }
}