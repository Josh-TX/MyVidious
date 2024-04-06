using MyVidious.Models.Invidious;

namespace MyVidious.Models.Admin;

public class FoundPlaylist : SearchResponse_Playlist
{
    public int? MyvidiousPlaylistId { get; set; }
    public string? ThumbnailUrl { get; set; }
}