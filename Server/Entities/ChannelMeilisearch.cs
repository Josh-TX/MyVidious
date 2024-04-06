using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

public class ChannelMeilisearch
{
    public required int Id { get; set; }
    /// <summary>
    /// Id2 is needed because you can't filter off the Primary Key
    /// </summary>
    public required int Id2 { get; set; }
    public required string Name { get; set; }
    public string? Handle { get; set; }
    public string? Description { get; set; }
}