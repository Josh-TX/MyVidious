using Meilisearch;
using MyVidious.Data;
using MyVidious.Models;
using MyVidious.Models.Invidious;
using System.Linq;

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

    public async Task AddPlaylistId(IEnumerable<int> videoIds, int playlistId)
    {
        var client = GetClient();
        if (_mainIndexConfigured != true)
        {
            await ConfigureMainIndex(client);
        }
        var index = client.Index(MAIN_INDEX);
        var tasks = videoIds.Select(z => index.GetDocumentAsync<MeilisearchStoredItem>(z)).ToList();
        await Task.WhenAll(tasks);
        var documents = tasks.Select(z => z.Result).ToList();
        foreach(var document in documents)
        {
            document.FilterIds = document.FilterIds.Append(playlistId + FILTER_PLAYLIST_ID_OFFSET).Distinct();
        }
        await index.UpdateDocumentsAsync(documents);
    }

    public async Task AddChannelId(IEnumerable<int> videoIds, int channelId)
    {
        var client = GetClient();
        if (_mainIndexConfigured != true)
        {
            await ConfigureMainIndex(client);
        }
        var index = client.Index(MAIN_INDEX);
        var tasks = videoIds.Select(z => index.GetDocumentAsync<MeilisearchStoredItem>(z)).ToList();
        await Task.WhenAll(tasks);
        var documents = tasks.Select(z => z.Result).ToList();
        foreach (var document in documents)
        {
            document.FilterIds = document.FilterIds.Append(channelId).Distinct();
        }
        await index.UpdateDocumentsAsync(documents);
    }

    public async Task RemovePlaylistId(IEnumerable<int> videoIds, int playlistId)
    {
        var filterPlaylistId = playlistId + FILTER_PLAYLIST_ID_OFFSET;
        var client = GetClient();
        if (_mainIndexConfigured != true)
        {
            await ConfigureMainIndex(client);
        }
        var index = client.Index(MAIN_INDEX);
        var tasks = videoIds.Select(z => index.GetDocumentAsync<MeilisearchStoredItem>(z)).ToList();
        await Task.WhenAll(tasks);
        var documents = tasks.Select(z => z.Result).ToList();
        var documentsToUpdate = new List<MeilisearchStoredItem>();
        foreach (var document in documents)
        {
            if (document.FilterIds.Contains(filterPlaylistId))
            {
                document.FilterIds = document.FilterIds.Where(z => z != filterPlaylistId);
                documentsToUpdate.Add(document);
            }
        }
        await index.UpdateDocumentsAsync(documentsToUpdate);
    }

    public async Task RemoveVideo(int videoId)
    {
        var client = GetClient();
        if (_mainIndexConfigured != true)
        {
            await ConfigureMainIndex(client);
        }
        var index = client.Index(MAIN_INDEX);
        try
        {
            await index.DeleteOneDocumentAsync(videoId);
        }
        catch (Exception) { }
    }

    public async Task<IEnumerable<MeilisearchItem>> SearchItems(string searchText, int? page, ChannelAndPlaylistIds channelAndPlaylistIds, MeilisearchType? type = null)
    {
        searchText = searchText.Trim();
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
        page ??= 1;
        var searchable = await index.SearchAsync<MeilisearchStoredItem>(searchText, new SearchQuery
        {
            Filter = filter,
            HitsPerPage = 20,
            Page = page.Value
        });
        if (page == 1 && searchable.Hits.Count == 0 && searchText.Contains(" "))
        {
            //meilisearch 1.7 has very bizare matching logic wherein all results must contain the first word, but other words are optional
            //I regret choosing meilisearch, but I've invested too much time in it to change to something else. Maybe a future update will fix this. 
            //For now, I'll mitgate the issue by searching again, but removing the first word (so now the 2nd word is required to match). 
            searchable = await index.SearchAsync<MeilisearchStoredItem>(searchText.Substring(searchText.IndexOf(" ") + 1), new SearchQuery
            {
                Filter = filter,
                HitsPerPage = 20,
                Page = page.Value
            });
        }
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
        if (item.FilterChannelId.HasValue)
        {
            filterIds.Add(item.FilterChannelId.Value);
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
            channelId = (int)(item.Id - CHANNEL_ID_OFFSET);
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
    /// <summary>
    /// This should only be non-null when it's a Channel Type
    /// </summary>
    public int? ChannelId { get; set; }
    /// <summary>
    /// This should only be non-null when it's a Playlist Type
    /// </summary>
    public int? PlaylistId { get; set; }

    /// <summary>
    /// This is used for filtering to the algorithm items... so videos should specify this, and channels should specify this
    /// </summary>
    public int? FilterChannelId { get; set; }
    /// <summary>
    /// This is used for filtering to the algorithm items... so videos should specify this, and playlists should specify this
    /// </summary>
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
