using MyVidious.Access;
using MyVidious.Models.Invidious;

namespace MyVidious.Utilities;

public class ImageUrlUtility
{
    private readonly InvidiousUrlsAccess _invidiousUrlsAccess;
    private readonly bool _proxyImages;
    private readonly string _myVidiousApiUrl;

    public ImageUrlUtility(AppSettings appSettings, IHttpContextAccessor httpContextAccessor, InvidiousUrlsAccess invidiousUrlsAccess)
    {
        _invidiousUrlsAccess = invidiousUrlsAccess;
        _proxyImages = string.IsNullOrEmpty(appSettings.ExternalInvidiousUrl);
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
        if (videoThumbnail.Url.StartsWith(InvidiousUrlsAccess.STORAGE_URL))
        {
            var replacementUrl = _proxyImages ? _myVidiousApiUrl : _invidiousUrlsAccess.GetExternalInvidiousUrl();
            videoThumbnail.Url = videoThumbnail.Url.Replace(InvidiousUrlsAccess.STORAGE_URL, replacementUrl);
            return videoThumbnail;
        }
        var foundInvidiousUrl = _invidiousUrlsAccess.GetInternalUrls().FirstOrDefault(z => videoThumbnail.Url.StartsWith(z));
        if (foundInvidiousUrl != null)
        {
            var replacementUrl = _proxyImages ? _myVidiousApiUrl : _invidiousUrlsAccess.GetExternalInvidiousUrl();
            videoThumbnail.Url = videoThumbnail.Url.Replace(foundInvidiousUrl, _myVidiousApiUrl);
            return videoThumbnail;
        }
        bool startsWithHttpOrHttps = System.Text.RegularExpressions.Regex.IsMatch(videoThumbnail.Url, @"^https?://");
        if (!startsWithHttpOrHttps)
        {
            var replacementUrl = _proxyImages ? _myVidiousApiUrl : _invidiousUrlsAccess.GetExternalInvidiousUrl();
            videoThumbnail.Url = replacementUrl + "/" + videoThumbnail.Url.TrimStart('/');
        }
        return videoThumbnail;
    }

    public AuthorThumbnail FixImageUrl(AuthorThumbnail thumbnail)
    {
        var foundInvidiousUrl = _invidiousUrlsAccess.GetInternalUrls().FirstOrDefault(z => thumbnail.Url.StartsWith(z));
        if (foundInvidiousUrl != null)
        {
            var replacementUrl = _proxyImages ? _myVidiousApiUrl : _invidiousUrlsAccess.GetExternalInvidiousUrl();
            thumbnail.Url = thumbnail.Url.Replace(foundInvidiousUrl, _myVidiousApiUrl);
            return thumbnail;
        }
        bool startsWithHttpOrHttps = System.Text.RegularExpressions.Regex.IsMatch(thumbnail.Url, @"^https?://");
        if (!startsWithHttpOrHttps)
        {
            var replacementUrl = _proxyImages ? _myVidiousApiUrl : _invidiousUrlsAccess.GetExternalInvidiousUrl();
            thumbnail.Url = replacementUrl + "/" + thumbnail.Url.TrimStart('/');
        }
        return thumbnail;
    }
}