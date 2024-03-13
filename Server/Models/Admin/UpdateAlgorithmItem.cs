namespace MyVidious.Models.Admin;

public class UpdateAlgorithmItem
{
    public int? ChannelGroupId { get; set; }
    public int? ChannelId { get; set; }
    public FoundChannel? NewChannel { get; set; }
    public int MaxChannelWeight { get; set; }
    public float WeightMultiplier { get; set; }
}