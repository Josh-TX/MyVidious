namespace MyVidious.Models.Invidious;

public class VideoThumbnail
{
    public string? Quality { get; set; }
    public required string Url { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}