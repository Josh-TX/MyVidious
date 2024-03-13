namespace MyVidious.Models.Admin;

public class FoundChannel
{
    public int? ChannelId { get; set; }
    public string UniqueId { get; set; }
    public string Name { get; set; }
    public string Handle { get; set; }
    public string Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? VideoCount { get; set; }
}