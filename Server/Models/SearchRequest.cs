
namespace MyVidious.Models;

public class SearchRequest {
    /// <summary>
    /// the query text
    /// </summary>
    public string? Q {get;set;}
    public int? Page {get;set;}
    /// <summary>
    /// possible values: "relevance", "rating", "upload_date", "view_count"
    /// </summary>
    public string? Sort_By {get;set;}

    /// <summary>
    /// possible values: "hour", "today", "week", "month", "year"
    /// </summary>
    public string? Date {get;set;}

    /// <summary>
    /// possible values: "short", "long", "medium"
    /// </summary>
    public string? Duration {get;set;}

    /// <summary>
    /// possible values: "video", "playlist", "channel", "movie", "show", "all"
    /// </summary>
    public string? Type {get;set;}

    /// <summary>
    /// possible values:  "hd", "subtitles", "creative_commons", "3d", "live", "purchased", "4k", "360", "location", "hdr", "vr180" (comma separated: e.g. "&features=hd,subtitles,3d,live")
    /// </summary>
    public string? Features {get;set;}

    /// <summary>
    /// ISO 3166 country code (default: "US")
    /// </summary>
    public string? Region {get;set;}
}
