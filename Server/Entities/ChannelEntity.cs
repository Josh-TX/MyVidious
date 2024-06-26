using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("channel")]
public class ChannelEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    /// <summary>
    /// should match the Author of the invidious API
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// should match the AuthorId of the invidious API
    /// </summary>
    public required string UniqueId { get; set; }
    public string? Handle { get; set; }


    public string? Description { get; set; }
    public string? AuthorUrl { get; set; }
    public string? ThumbnailsJson { get; set; }

    public bool AuthorVerified { get; set; }
    public bool AutoGenerated { get; set; }
    public int SubCount { get; set; }
    public int VideoCount { get; set; }

    public required string AddedByUser { get; set; }

    /// <summary>
    /// When true, indicates that we've scraped all the way to the oldest. This is very important, because now anytime we scrap, we can stop as soon as we encounter an existing video
    /// </summary>
    public bool ScrapedToOldest { get; set; }
    public DateTime? DateLastScraped { get; set; }
    public short ScrapeFailureCount { get; set; }


    public IList<VideoEntity>? Videos { get; set; }
}