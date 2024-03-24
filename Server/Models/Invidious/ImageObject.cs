namespace MyVidious.Models.Invidious;

public class ImageObject
{
    public required string Url { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}