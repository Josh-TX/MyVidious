using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

public class VideoMeilisearch
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ChannelName { get; set; }
    public string ChannelHandle { get; set; }
}