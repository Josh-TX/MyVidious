using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("ChannelGroupItem")]
public class ChannelGroupItemEntity
{
    public int ChannelGroupId { get; set; }
    public int ChannelId { get; set; }
}