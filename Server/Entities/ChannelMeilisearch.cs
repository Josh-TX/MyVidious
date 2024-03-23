using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

public class ChannelMeilisearch
{
    public required int Id { get; set; }
    public required string ChannelName { get; set; }
}