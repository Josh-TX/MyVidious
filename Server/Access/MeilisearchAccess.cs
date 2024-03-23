using Meilisearch;
using MyVidious.Data;
using MyVidious.Models;
using MyVidious.Models.Invidious;

namespace MyVidious.Access;

public class MeilisearchAccess
{
    private string _meilisearchUrl;
    private bool? _videoIndexConfigured;

    public MeilisearchAccess(AppSettings appsettings)
    {
        _meilisearchUrl = appsettings.MeilisearchUrl!;
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
            var res = await index.AddDocumentsAsync(videos);
            var status = res.Status;
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
        //var filter = "channelId = " + channelIds.First();
        var searchable = await index.SearchAsync<VideoMeilisearch>(searchText, new SearchQuery
        {
            Filter = filter,
            HitsPerPage = 20,
            Page = page.HasValue ? page.Value : 1
        });
        var videoIds = searchable.Hits.Select(z => z.Id).ToList();
        return videoIds;
    }

    //public async Task AddChannels(IEnumerable<VideoMeilisearch> videos)
    //{
    //    try
    //    {
    //        var client = GetClient();
    //        Meilisearch.Index index;
    //        //I want to avoid calling UpdateSearchableAttributesAsync and UpdateFilterableAttributesAsync.
    //        //To avoid awaiting GetAllIndexesAsync(), I'll use _indexConfigured so that each server restart it only does so once. 
    //        if (_indexConfigured != true)
    //        {
    //            await ConfigureVideoIndex(client);
    //        }
    //        index = client.Index("videos");
    //        var res = await index.AddDocumentsAsync(videos);
    //        var status = res.Status;
    //    }
    //    catch (Exception)
    //    {

    //    }
    //}

    private MeilisearchClient GetClient()
    {
        return new MeilisearchClient(_meilisearchUrl, "aSampleMasterKey");
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
}
