using MyVidious.Models.Invidious;

namespace MyVidious.Models.Admin;

public class UpdateAlgorithmItem
{
    public int? ChannelId { get; set; }
    public SearchResponse_Channel? NewChannel { get; set; }
    public int? PlaylistId { get; set; }
    public string? Folder { get; set; }
    public SearchResponse_Playlist? NewPlaylist { get; set; }
    public float WeightMultiplier { get; set; }
}