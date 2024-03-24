namespace MyVidious.Models.Invidious; 

public class AuthorThumbnail
{
    public required string Url { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}