namespace MyVidious.Models;

/// <summary>
/// Should match the response of /api/v1/channels/{channelId}/videos
/// </summary>
public class ChannelVideosResponse
{
    public IEnumerable<VideoObject> Videos { get; set; }

    /// <summary>
    /// If there are no more videos to load, this will be null/missing
    /// </summary>
    public string? Continuation { get; set; }

}