using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("video")]
public class VideoEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int? ChannelId { get; set; }
    public required string Title { get; set; }
    /// <summary>
    /// should match the VideoId of the invidious API
    /// </summary>
    public required string UniqueId { get; set; }

    public required string Author { get; set; }
    public required string AuthorId { get; set; }
    public string? AuthorUrl { get; set; }
    public bool AuthorVerified { get; set; }

    public string? ThumbnailsJson { get; set; }
    public string? Description { get; set; }

    public long ViewCount { get; set; }
    public int LengthSeconds { get; set; }

    public long? EstimatedPublished { get; set; }
    public long? ActualPublished { get; set; }

    public long? PremiereTimestamp { get; set; }
    public bool LiveNow { get; set; }
    public bool Premium { get; set; }
    public bool IsUpcoming { get; set; }

    public short FailureCount { get; set; }


    [ForeignKey("ChannelId")] 
    public ChannelEntity? Channel { get; set; }
    public IList<PlaylistVideoEntity>? PlaylistVideos { get; set; }
}