using MyVidious.Models.Invidious;

namespace MyVidious.Models.Admin;

public class FoundChannel : SearchResponse_Channel
{
    public int? ChannelId { get; set; }
    public string ThumbnailUrl { get; set; }
}