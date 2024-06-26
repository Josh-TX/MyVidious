using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("algorithm_item")]
public class AlgorithmItemEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int AlgorithmId { get; set; }
    public int? PlaylistId { get; set; }
    public int? ChannelId { get; set; }
    public string? Folder { get; set; }
    public double WeightMultiplier { get; set; }

    [ForeignKey("AlgorithmId")]
    public AlgorithmEntity? Algorithm { get; set; }
}