using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

public class AlgorithmItemInfoEntity
{
    public int AlgorithmId { get; set; }
    public int? PlaylistId { get; set; }
    public int? ChannelId { get; set; }
    public double WeightMultiplier { get; set; }
    public string? Folder { get; set; }
    public int MaxItemWeight { get; set; }
    public required string Name { get; set; }
    public int VideoCount { get; set; }
    public int FailureCount { get; set; }
}