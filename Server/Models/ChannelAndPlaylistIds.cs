namespace MyVidious.Models
{
    public class ChannelAndPlaylistIds
    {
        public required IEnumerable<int> ChannelIds { get; set; }
        public required IEnumerable<int> PlaylistIds { get; set; }
    }
}
