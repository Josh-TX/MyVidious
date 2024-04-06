namespace MyVidious.Models.Admin;

public class LoadAlgorithmItem
{
    public int? PlaylistId { get; set; }
    public int? ChannelId { get; set; }
    public required double WeightMultiplier { get; set; }
    public required string Name { get; set; }
    public string? Folder { get; set; }
    public int? VideoCount { get; set; }
    public required int FailureCount { get; set; }
    public required double EstimatedWeight { get; set; }
}
