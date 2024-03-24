using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace MyVidious.Models.Invidious;

//Attempts to use the `Type` property as a TypeDiscriminator didn't work properly. The Json serialization added a null type property

/// <summary>
/// Needed to deserialize with SearchResponseBase support of derrived types.
/// </summary>
public class SearchResponseConverter : JsonConverter
{ 
    public override bool CanConvert(Type objectType)
    {
        return typeof(SearchResponseBase).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);
        string type = jsonObject["type"]!.Value<string>()!;

        SearchResponseBase target = type switch
        {
            "video" => new SearchResponse_Video()
            {
                Type = "video",
                Author = "",
                AuthorId = "",
                Title = "",
                VideoId = ""
            },
            "channel" => new SearchResponse_Channel()
            {
                Type = "channel",
                Author = "",
                AuthorId = ""
            },
            "playlist" => new SearchResponse_Playlist()
            {
                Type = "playlist",
                Title = "",
                PlaylistId = "",
            },
            _ => throw new ArgumentException($"Invalid search result type: {type}"),
        };

        serializer.Populate(jsonObject.CreateReader(), target);
        return target;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Should match the response of /api/v1/search
/// </summary>
public class SearchResponseBase {
    public required string Type {get;set;}
}

//this is basically identical to the video object. But I don't wanna deal with the Type property being on the base type
public class SearchResponse_Video : SearchResponseBase
{
    public required string Title { get; set; }
    public required string VideoId { get; set; }

    public required string Author { get; set; }
    public required string AuthorId { get; set; }
    public string? AuthorUrl { get; set; }
    public bool AuthorVerified { get; set; }

    public IEnumerable<VideoThumbnail>? VideoThumbnails { get; set; }
    public string? Description { get; set; }
    public string? DescriptionHtml { get; set; }

    public long ViewCount { get; set; }
    public string? ViewCountText { get; set; }
    public int LengthSeconds { get; set; }

    public long Published { get; set; }
    public string? PublishedText { get; set; }


    public bool? Paid { get; set; } //in documentation but not API
    public bool LiveNow { get; set; }
    public bool Premium { get; set; }
    public bool IsUpcoming { get; set; }
}

public class SearchResponse_Channel : SearchResponseBase
{
    public string? ChannelHandle { get; set; }
    public string? Description { get; set; }
    public string? DescriptionHtml { get; set; }
    public required string Author { get; set; } 
    public required string AuthorId { get; set; }
    public string? AuthorUrl { get; set; }
    public IEnumerable<AuthorThumbnail>? AuthorThumbnails {get;set;}
    public bool AuthorVerified {get;set;}
    public bool AutoGenerated {get;set;}
    public int SubCount {get;set;}
    public int VideoCount {get;set;}
}

public class SearchResponse_Playlist : SearchResponseBase
{
    public required string Title { get; set; }
    public required string PlaylistId { get; set; }
    public string? PlaylistThumbnail { get; set; }
    public string? Author { get; set; }
    public string? AuthorId { get; set; }
    public string? AuthorUrl { get; set; }
    public bool AuthorVerified {get;set;}
    public int VideoCount {get;set;}
    public IEnumerable<PlaylistVideo>? Videos {get;set;}
}

public class PlaylistVideo {
    public int LengthSeconds {get;set;}
    public required string Title {get;set;}
    public required string VideoId { get; set; }
    public IEnumerable<VideoThumbnail>? VideoThumbnails { get; set; }
}