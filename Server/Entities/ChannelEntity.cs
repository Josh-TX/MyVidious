using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("Channel")]
public class ChannelEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public string UniqueId { get; set; }
    public string Handle { get; set; }

    /// <summary>
    /// When true, indicates that we've scraped all the way to the oldest. This is very important, because now anytime we scrap, we can stop as soon as we encounter an existing video
    /// </summary>
    public bool ScrapedToOldest { get; set; }
    public DateTime? DateLastScraped { get; set; }
    public short ScrapeFailureCount { get; set; }


    public IList<VideoEntity> Videos { get; set; }
}