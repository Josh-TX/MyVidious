namespace MyVidious.Models; 

public class ChannelObject
{
    public string Author { get; set; }
    public string AuthorId { get; set; }
    public string AuthorUrl { get; set; }
    public IEnumerable<AuthorThumbnail> AuthorThumbnails { get; set; }
}