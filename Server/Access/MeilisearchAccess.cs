using Meilisearch;
using MyVidious.Data;
using MyVidious.Models;
using MyVidious.Models.Invidious;

namespace MyVidious.Access;

public class MeilisearchAccess
{
    private string _meilisearchUrl;
    private string _meilisearchKey;
    private bool? _videoIndexConfigured;
    private bool? _channelIndexConfigured;

    public MeilisearchAccess(AppSettings appsettings)
    {
        _meilisearchUrl = appsettings.MeilisearchUrl!;
        _meilisearchKey = appsettings.MeilisearchKey ?? "";
    }

    public async Task AddVideos(IEnumerable<VideoMeilisearch> videos)
    {
        try
        {
            var client = GetClient();
            Meilisearch.Index index;
            //I want to avoid calling UpdateSearchableAttributesAsync and UpdateFilterableAttributesAsync.
            //To avoid awaiting GetAllIndexesAsync(), I'll use _indexConfigured so that each server restart it only does so once. 
            if (_videoIndexConfigured != true)
            {
                await ConfigureVideoIndex(client);
            }
            index = client.Index("videos");
            await index.AddDocumentsAsync(videos);
        }
        catch (Exception)
        {

        }
    }

    public async Task<IEnumerable<int>> SearchVideoIds(string searchText, int? page, IEnumerable<int> channelIds)
    {
        var client = GetClient();
        if (_videoIndexConfigured != true)
        {
            await ConfigureVideoIndex(client);
        }
        var index = client.Index("videos");
        var filter = "channelId IN [" + string.Join(',', channelIds) + "]";
        var searchable = await index.SearchAsync<VideoMeilisearch>(searchText, new SearchQuery
        {
            Filter = filter,
            HitsPerPage = 20,
            Page = page.HasValue ? page.Value : 1
        });
        var videoIds = searchable.Hits.Select(z => z.Id).ToList();
        return videoIds;
    }

    public async Task AddChannels(IEnumerable<ChannelMeilisearch> channels)
    {
        var client = GetClient();
        Meilisearch.Index index;
        if (_channelIndexConfigured != true)
        {
            await ConfigureChannelIndex(client);
        }
        index = client.Index("channels");
        await index.AddDocumentsAsync(channels);
    }

    public async Task<IEnumerable<int>> SearchChannelIds(string searchText, int? page, IEnumerable<int> channelIds, bool searchDescription)
    {
        var client = GetClient();
        if (_channelIndexConfigured != true)
        {
            await ConfigureChannelIndex(client);
        }
        var index = client.Index("channels");
        var filter = "id2 IN [" + string.Join(',', channelIds) + "]";
        var query = new SearchQuery
        {
            Filter = filter,
            HitsPerPage = 20,
            Page = page.HasValue ? page.Value : 1
        };
        if (!searchDescription)
        {
            query.AttributesToSearchOn = new[] { "name", "handle" };
        }
        var searchable = await index.SearchAsync<ChannelMeilisearch>(searchText, query);
        var foundChannelIds = searchable.Hits.Select(z => z.Id).ToList();
        return foundChannelIds;
    }

    private MeilisearchClient GetClient()
    {
        return new MeilisearchClient(_meilisearchUrl, _meilisearchKey);
    }

    private async Task ConfigureVideoIndex(MeilisearchClient client)
    {
        var indexes = await client.GetAllIndexesAsync();
        var videoIndex = indexes.Results.FirstOrDefault(z => z.Uid == "videos");
        if (videoIndex == null)
        {
            await client.CreateIndexAsync("videos", "id");
            var newIndex = client.Index("videos");
            await newIndex.UpdateSearchableAttributesAsync(new[] { "title", "channelName" });
            await newIndex.UpdateFilterableAttributesAsync(new[] { "channelId" });
        }
        _videoIndexConfigured = true;
    }

    private async Task ConfigureChannelIndex(MeilisearchClient client)
    {
        var indexes = await client.GetAllIndexesAsync();
        var channelIndex = indexes.Results.FirstOrDefault(z => z.Uid == "channels");
        if (channelIndex == null)
        {
            await client.CreateIndexAsync("channels", "id");
            var newIndex = client.Index("channels");
            await newIndex.UpdateSearchableAttributesAsync(new[] { "name", "handle", "description" });
            await newIndex.UpdateFilterableAttributesAsync(new[] { "id2" });
        }
        _channelIndexConfigured = true;
    }
}
