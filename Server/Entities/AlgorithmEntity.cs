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

    //the next 3 columns aren't used, but since it's hard to deploy DB updates I'm making them now

    /// <summary>
    /// Unused at the moment, but may be used later to prevent watching videos not on the algorithm. Currently possible via viewing the channel of a playlist video
    /// </summary>
    public bool IsRestricted { get; set; }
    /// <summary>
    /// when > 0, and showing a video from a playlist, the first recommendation will be the next video in the playlist
    /// </summary>
    public int BiasCurrentPlaylist { get; set; }
    /// <summary>
    /// when > 0, and showing a video from a channel,  one of the first N recommendations will be propogated from youtube's algorithm matching the channel
    /// </summary>
    public int BiasCurrentChannel { get; set; }

    public IList<AlgorithmItemEntity>? AlgorithmItems { get; set; }
}