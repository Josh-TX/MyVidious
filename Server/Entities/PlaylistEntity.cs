using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("playlist")]
public class PlaylistEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string Title { get; set; }
    /// <summary>
    /// should match the PlaylistId of the invidious API
    /// </summary>
    public required string UniqueId { get; set; }
    public int VideoCount { get; set; }

    public string? PlaylistThumbnail { get; set; }
    public string? Author { get; set; }
    public string? AuthorId { get; set; }
    public string? AuthorUrl { get; set; }

    public DateTime? DateLastScraped { get; set; }
    public short ScrapeFailureCount { get; set; }


    public IList<PlaylistVideoEntity>? PlaylistVideos { get; set; }
}