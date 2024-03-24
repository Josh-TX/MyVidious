using MyVidious.Models.Invidious;

namespace MyVidious.Models.Admin;

public class UpdateAlgorithmItem
{
    public int? ChannelGroupId { get; set; }
    public int? ChannelId { get; set; }
    public SearchResponse_Channel? NewChannel { get; set; }
    public int MaxChannelWeight { get; set; }
    public float WeightMultiplier { get; set; }
}