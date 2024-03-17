using MyVidious.Models.Invidious;

namespace MyVidious.Utilities;

public class ImageUrlUtility
{
    private readonly string _invidiousUrl;
    private readonly bool _proxyImages;
    private readonly string _myVidiousUrl;

    public ImageUrlUtility(AppSettings appSettings, IHttpContextAccessor httpContextAccessor)
    {
        _invidiousUrl = appSettings.InvidiousUrl.TrimEnd('/');
        _proxyImages = appSettings.ProxyImages;
        var request = httpContextAccessor.HttpContext!.Request;
        _myVidiousUrl = $"https://{request.Host}".TrimEnd('/');
    }

    /// <summary>
    /// Should be called prior to storing an image URL
    /// </summary>
    public static VideoThumbnail MakeUrlRelative(VideoThumbnail videoThumbnail, string invidiousUrl)
    {
        if (videoThumbnail.Url.StartsWith(invidiousUrl))
        {
            videoThumbnail.Url = videoThumbnail.Url.Substring(invidiousUrl.Length);
        }
        return videoThumbnail;
    }


    /// <summary>
    /// Should be called prior to returning an image URL
    /// </summary>
    public VideoThumbnail FixImageUrl(VideoThumbnail videoThumbnail)
    {
        if (videoThumbnail.Url.StartsWith(_invidiousUrl))
        {
            if (_proxyImages)
            {
                videoThumbnail.Url = videoThumbnail.Url.Replace(_invidiousUrl, _myVidiousUrl + "/a/a");
            }
            return videoThumbnail;
        }
        bool startsWithHttpOrHttps = System.Text.RegularExpressions.Regex.IsMatch(videoThumbnail.Url, @"^https?://");
        if (!startsWithHttpOrHttps)
        {
            var replacementUrl = _proxyImages ? _myVidiousUrl + "/a/a" : _invidiousUrl;
            videoThumbnail.Url = replacementUrl + "/" + videoThumbnail.Url.TrimStart('/');
        }
        return videoThumbnail;
    }
}