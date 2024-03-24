using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

public class AlgorithmItemInfoEntity
{
    public int AlgorithmId { get; set; }
    public int? ChannelGroupId { get; set; }
    public int? ChannelId { get; set; }
    public double WeightMultiplier { get; set; }
    public int MaxChannelWeight { get; set; }
    public required string Name { get; set; }
    public int? VideoCount { get; set; }
    public int FailureCount { get; set; }
    public int? ChannelCount { get; set; }
}