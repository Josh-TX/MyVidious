using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

public class ChannelMeilisearch
{
    public required int Id { get; set; }
    public required int Id2 { get; set; }
    public required string Name { get; set; }
    public required string Handle { get; set; }
    public required string Description { get; set; }
}