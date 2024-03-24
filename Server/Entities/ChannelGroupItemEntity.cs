using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("channel_group_item")]
public class ChannelGroupItemEntity
{
    public int ChannelGroupId { get; set; }
    public int ChannelId { get; set; }

    public ChannelEntity? Channel { get; set; }
}