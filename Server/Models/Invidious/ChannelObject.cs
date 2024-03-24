namespace MyVidious.Models.Invidious;

public class ChannelObject
{
    public required string Author { get; set; }
    public required string AuthorId { get; set; }
    public string? AuthorUrl { get; set; }
    public IEnumerable<AuthorThumbnail>? AuthorThumbnails { get; set; }
}