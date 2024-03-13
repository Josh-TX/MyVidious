namespace MyVidious.Models.Invidious;

/// <summary>
/// Should match the available input parameters of /api/v1/channels/{channelId}/videos
/// </summary>
public class ChannelVideosRequest
{
    /// <summary>
    /// possible values: newest, popular, oldest
    /// </summary>
    public string? Sort_by { get; set; }
    public string? Continuation { get; set; }

}