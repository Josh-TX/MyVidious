using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("channel_group")]
public class ChannelGroupEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int ChannelCount { get; set; }

    public IList<ChannelGroupItemEntity>? Items { get; set; }
}