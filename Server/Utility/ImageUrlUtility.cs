using MyVidious.Access;
using MyVidious.Models.Invidious;

namespace MyVidious.Utilities;

public class ImageUrlUtility
{
    private readonly string _invidiousUrl;
    private readonly bool _proxyImages;
    private readonly string _myVidiousApiUrl;

    public ImageUrlUtility(AppSettings appSettings, IHttpContextAccessor httpContextAccessor, InvidiousUrlsAccess invidiousUrlsAccess)
    {
        _invidiousUrl = invidiousUrlsAccess.GetInvidiousUrl();
        _proxyImages = appSettings.ProxyImages;
        var request = httpContextAccessor.HttpContext!.Request;
        var segments = request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var path = segments != null && segments.Length >= 2
            ? $"/{segments[0]}/{segments[1]}"
            : "/placeholder/placeholder";
        _myVidiousApiUrl = $"{request.Scheme}://{request.Host}" + path;
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
                videoThumbnail.Url = videoThumbnail.Url.Replace(_invidiousUrl, _myVidiousApiUrl);
            }
            return videoThumbnail;
        }
        bool startsWithHttpOrHttps = System.Text.RegularExpressions.Regex.IsMatch(videoThumbnail.Url, @"^https?://");
        if (!startsWithHttpOrHttps)
        {
            var replacementUrl = _proxyImages ? _myVidiousApiUrl : _invidiousUrl;
            videoThumbnail.Url = replacementUrl + "/" + videoThumbnail.Url.TrimStart('/');
        }
        return videoThumbnail;
    }

    public AuthorThumbnail FixImageUrl(AuthorThumbnail thumbnail)
    {
        if (thumbnail.Url.StartsWith(_invidiousUrl))
        {
            if (_proxyImages)
            {
                thumbnail.Url = thumbnail.Url.Replace(_invidiousUrl, _myVidiousApiUrl);
            }
            return thumbnail;
        }
        bool startsWithHttpOrHttps = System.Text.RegularExpressions.Regex.IsMatch(thumbnail.Url, @"^https?://");
        if (!startsWithHttpOrHttps)
        {
            if (thumbnail.Url.StartsWith("//"))
            {
                thumbnail.Url = "https:" + thumbnail.Url;
            } else
            {
                var replacementUrl = _proxyImages ? _myVidiousApiUrl : _invidiousUrl;
                thumbnail.Url = replacementUrl + "/" + thumbnail.Url.TrimStart('/');
            }
        }
        return thumbnail;
    }
}