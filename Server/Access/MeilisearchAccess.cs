using Meilisearch;
using MyVidious.Data;
using MyVidious.Models;
using MyVidious.Models.Invidious;

namespace MyVidious.Access;

public class MeilisearchAccess
{
    private string _meilisearchUrl;
    private string _meilisearchKey;
    private bool? _mainIndexConfigured;
    private const string MAIN_INDEX = "main";
    private const uint CHANNEL_ID_OFFSET = 2147483647u;
    private const uint PLAYLIST_ID_OFFSET = 3221225471u;
    private const int FILTER_PLAYLIST_ID_OFFSET = 1073741823;

    public MeilisearchAccess(AppSettings appsettings)
    {
        _meilisearchUrl = appsettings.MeilisearchUrl!;
        _meilisearchKey = appsettings.MeilisearchKey ?? "";
    }

    public async Task AddItems(IEnumerable<MeilisearchItem> items)
    {
        try
        {
            var itemsToStore = items.Select(TranslateToStored).ToList();
            var client = GetClient();
            Meilisearch.Index index;
            //I want to avoid calling UpdateSearchableAttributesAsync and UpdateFilterableAttributesAsync.
            //To avoid awaiting GetAllIndexesAsync(), I'll use _indexConfigured so that each server restart it only does so once. 
            if (_mainIndexConfigured != true)
            {
                await ConfigureMainIndex(client);
            }
            index = client.Index(MAIN_INDEX);
            await index.AddDocumentsAsync<MeilisearchStoredItem>(itemsToStore);
        }
        catch (Exception)
        {

        }
    }

    public async Task<IEnumerable<MeilisearchItem>> SearchItems(string searchText, int? page, ChannelAndPlaylistIds channelAndPlaylistIds, MeilisearchType? type = null)
    {
        var client = GetClient();
        if (_mainIndexConfigured != true)
        {
            await ConfigureMainIndex(client);
        }
        var index = client.Index(MAIN_INDEX);
        var filterIds = channelAndPlaylistIds.ChannelIds.Concat(channelAndPlaylistIds.PlaylistIds.Select(z => z + FILTER_PLAYLIST_ID_OFFSET)).ToList();
        var filter = "filterIds IN [" + string.Join(',', filterIds) + "]";
        if (type.HasValue)
        {
            filter = $"type = {(int)type} AND " + filter;
        }
        var searchable = await index.SearchAsync<MeilisearchStoredItem>(searchText, new SearchQuery
        {
            Filter = filter,
            HitsPerPage = 20,
            Page = page.HasValue ? page.Value : 1
        });
        var items = searchable.Hits.Select(TranslateFromStored).ToList();
        return items;
    }

    private MeilisearchClient GetClient()
    {
        return new MeilisearchClient(_meilisearchUrl, _meilisearchKey);
    }

    private async Task ConfigureMainIndex(MeilisearchClient client)
    {
        var indexes = await client.GetAllIndexesAsync();
        var videoIndex = indexes.Results.FirstOrDefault(z => z.Uid == MAIN_INDEX);
        if (videoIndex == null)
        {
            //meilisearch seems very picky about this being camelCase
            await client.CreateIndexAsync(MAIN_INDEX, "id");
            var newIndex = client.Index(MAIN_INDEX);
            await newIndex.UpdateSearchableAttributesAsync(new[] { "name", "secondName" });
            await newIndex.UpdateFilterableAttributesAsync(new[] { "filterIds", "type" });
        }
        _mainIndexConfigured = true;
    }

    private MeilisearchStoredItem TranslateToStored(MeilisearchItem item)
    {
        uint id = item.VideoId.HasValue ? (uint)item.VideoId
            : item.ChannelId.HasValue ? (uint)item.ChannelId + CHANNEL_ID_OFFSET : (uint)item.PlaylistId!.Value + PLAYLIST_ID_OFFSET;
        MeilisearchType type = item.VideoId.HasValue ? MeilisearchType.Video
            : item.ChannelId.HasValue ? MeilisearchType.Channel : MeilisearchType.Playlist;
        var filterIds = item.FilterPlaylistIds?.Select(z => z + FILTER_PLAYLIST_ID_OFFSET).ToList() ?? new List<int>();
        if (item.ChannelId.HasValue)
        {
            filterIds.Add(item.ChannelId.Value);
        }
        return new MeilisearchStoredItem
        {
            Id = id,
            FilterIds = filterIds,
            Type = (int)type,
            Name = item.Name,
            SecondName = item.SecondName
        };
    }

    private MeilisearchItem TranslateFromStored(MeilisearchStoredItem item)
    {
        int? videoId = null, channelId = null, playlistId = null;
        if (item.Id > PLAYLIST_ID_OFFSET)
        {
            playlistId = (int)(item.Id - PLAYLIST_ID_OFFSET);
        }
        else if (item.Id > CHANNEL_ID_OFFSET)
        {
            playlistId = (int)(item.Id - CHANNEL_ID_OFFSET);
        } else
        {
            videoId = (int)item.Id;
        }
        int filterChannelId = item.FilterIds.FirstOrDefault(z => z <= FILTER_PLAYLIST_ID_OFFSET);
        return new MeilisearchItem
        {
            VideoId = videoId,
            ChannelId = channelId,
            PlaylistId = playlistId,
            FilterChannelId = filterChannelId == 0 ? null : filterChannelId,
            FilterPlaylistIds = item.FilterIds.Where(z => z > FILTER_PLAYLIST_ID_OFFSET).Select(z => z - FILTER_PLAYLIST_ID_OFFSET),
            Name = item.Name,
            SecondName = item.SecondName
        };
    }
}

public enum MeilisearchType
{
    Video = 1,
    Channel = 2,
    Playlist = 3,
}

/// <summary>
/// This class makes it easier to interact with MeilisearchStoredItem
/// </summary>
public class MeilisearchItem
{
    //precisely 1 of these 3 fields should be null
    public int? VideoId { get; set; }
    public int? ChannelId { get; set; }
    public int? PlaylistId { get; set; }


    public int? FilterChannelId { get; set; }
    public IEnumerable<int>? FilterPlaylistIds { get; set; }


    /// <summary>
    /// This is the video title, channel name, or playlist title
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// This is the video's channelName or the channel's handle. Null for playlists.
    /// </summary>
    public string? SecondName { get; set; }
}



/// <summary>
/// This is what we store in Meilisearch. This model can represent a Video, Channel, or a Playlist
/// </summary>
public class MeilisearchStoredItem
{
    /// <summary>
    /// VideoIds range from 1 to 2147483647, ChannelIds range from 2147483648 to 3221225471, PlaylistIds range from 3221225472 to 4294967295
    /// </summary>
    public required uint Id { get; set; }

    /// <summary>
    /// Should match a value from the MeilisearchType enum
    /// </summary>
    public required int Type { get; set; }

    /// <summary>
    /// ChannelIds range from 1 to 1073741823, PlaylistIds range from 1073741823 to 2147483647
    /// </summary>
    public required IEnumerable<int> FilterIds { get; set; }
    /// <summary>
    /// This is the video title, channel name, or playlist title
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// This is the video's channelName or the channel's handle. Null for playlists.
    /// </summary>
    public string? SecondName { get; set; }
}
