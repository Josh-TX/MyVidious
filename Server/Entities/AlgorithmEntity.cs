using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("algorithm")]
public class AlgorithmEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int MaxItemWeight { get; set; }
    public bool IsListed { get; set; }
    public bool BiasCurrentChannel { get; set; }
    public bool BiasCurrentPlaylist { get; set; }

    /// <summary>
    /// Unused at the moment, but may be used later to prevent watching videos not on the algorithm. Currently possible via viewing the channel of a playlist video
    /// </summary>
    public bool IsRestricted { get; set; }

    public IList<AlgorithmItemEntity>? AlgorithmItems { get; set; }
}