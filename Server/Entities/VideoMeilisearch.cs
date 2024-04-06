using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

public class VideoMeilisearch
{
    public required int Id { get; set; }
    public required int? ChannelId { get; set; }
    public required string Title { get; set; }
    public required string ChannelName { get; set; }
}