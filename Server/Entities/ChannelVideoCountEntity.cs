using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("vw_ChannelVideoCount")]
public class ChannelVideoCountEntity
{
    public int ChannelId { get; set; }
    public string UniqueId { get; set; }
    public int? VideoCount { get; set; }
}