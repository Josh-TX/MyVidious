using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("AlgorithmItem")]
public class AlgorithmItemEntity
{
    public int AlgorithmId { get; set; }
    public int? ChannelGroupId { get; set; }
    public int? ChannelId { get; set; }
    public float WeightMultiplier { get; set; }
    public int MaxChannelWeight { get; set; }
}