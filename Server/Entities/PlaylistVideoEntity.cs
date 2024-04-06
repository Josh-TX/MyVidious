using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("playlist_video")]
public class PlaylistVideoEntity
{
    public int VideoId { get; set; }
    public int PlaylistId { get; set; }
    public int Index { get; set; }

    [ForeignKey("VideoId")]
    public VideoEntity? Video { get; set; }
    [ForeignKey("PlaylistId")]
    public PlaylistEntity? Playlist { get; set; }
}