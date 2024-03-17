using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyVidious.Models.Invidious;

/// <summary>
/// Should match the VideoObject in the documentation
/// </summary>
public class PopularVideo
{
    /// <summary>
    /// No idea why this is called "shortVideo"... you can have videos over an hour long in the "Popular" section
    /// </summary>
    public string Type { get; set; }
    public string Title { get; set; }


    public string VideoId { get; set; }
    public List<VideoThumbnail> VideoThumbnails { get; set; }


    public int LengthSeconds { get; set; }
    public long ViewCount { get; set; }

    public string Author { get; set; }
    public string AuthorId { get; set; }
    public string AuthorUrl { get; set; }


    public long Published { get; set; }
    public string PublishedText { get; set; }
}