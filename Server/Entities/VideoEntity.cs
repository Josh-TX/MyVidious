using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("video")]
public class VideoEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int ChannelId { get; set; }
    public string Title { get; set; }
    public string UniqueId { get; set; }

    public string Author { get; set; }
    public string AuthorId { get; set; }
    public string AuthorUrl { get; set; }
    public bool AuthorVerified { get; set; }

    public string ThumbnailsJson { get; set; }
    public string Description { get; set; }
    public string DescriptionHtml { get; set; }

    public long ViewCount { get; set; }
    public string ViewCountText { get; set; }
    public int LengthSeconds { get; set; }

    public long Published { get; set; }
    public string PublishedText { get; set; }

    public int? PremiereTimestamp { get; set; }
    public bool LiveNow { get; set; }
    public bool Premium { get; set; }
    public bool IsUpcoming { get; set; }

    [ForeignKey("ChannelId")] 
    public ChannelEntity Channel { get; set; }
}