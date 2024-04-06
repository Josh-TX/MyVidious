namespace MyVidious.Models.Invidious;

/// <summary>
/// Should match the response of /api/v1/channels/{channelId}
/// </summary>
public class PlaylistResponse
{
    public required string Title { get; set; }
    public required string Type { get; set; }
    public required string PlaylistId { get; set; }

    /// <summary>
    /// This is the user who made the playlist... might not even have any uploaded videos 
    /// </summary>
    public string? Author { get; set; }
    /// <summary>
    /// This is the user who made the playlist... might not even have any uploaded videos 
    /// </summary>
    public string? AuthorId { get; set; }
    public IEnumerable<AuthorThumbnail>? AuthorThumbnails { get; set; }
    public string? AuthorUrl { get; set; }


    public string? Description { get; set; }
    public string? DescriptionHtml { get; set; }

    public bool? IsListed { get; set; }
    public string? PlaylistThumbnail { get; set; }
    //public string Subtitle { get; set; }
    public long Updated { get; set; }
    public int VideoCount { get; set; }
    public required IEnumerable<PlaylistResponseVideo> Videos { get; set; }
    public long ViewCount { get; set; }

}

public class PlaylistResponseVideo
{
    public required string Title { get; set; }

    public int Index { get; set; }

    public required string VideoId { get; set; }
    public List<VideoThumbnail>? VideoThumbnails { get; set; }


    public int LengthSeconds { get; set; }

    public required string Author { get; set; }
    public required string AuthorId { get; set; }
    public string? AuthorUrl { get; set; }
}