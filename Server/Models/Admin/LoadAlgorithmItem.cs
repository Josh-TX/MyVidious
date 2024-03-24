namespace MyVidious.Models.Admin;

public class LoadAlgorithmItem
{
    public int? ChannelGroupId { get; set; }
    public int? ChannelId { get; set; }
    public required double WeightMultiplier { get; set; }
    public int MaxChannelWeight { get; set; }
    public required string Name { get; set; }
    public int? VideoCount { get; set; }
    public required int FailureCount { get; set; }
    public int? ChannelCount { get; set; }
    public required double EstimatedWeight { get; set; }
}
