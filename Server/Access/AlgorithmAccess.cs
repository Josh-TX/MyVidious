using MyVidious.Data;
using MyVidious.Models;
using MyVidious.Models.Invidious;
using MyVidious.Utilities;

namespace MyVidious.Access;

public class AlgorithmAccess
{
    private VideoDbContext _videoDbContext;
    private GlobalCache _globalCache;
    private IPScopedCache _ipScopedCache;
    public AlgorithmAccess(VideoDbContext videoDbContext, GlobalCache globalCache, IPScopedCache ipScopedCache)
    {
        _videoDbContext = videoDbContext;
        _globalCache = globalCache;
        _ipScopedCache = ipScopedCache;
    }

    public int? GetAlgorithmId(string username, string algorithmName)
    {
        return _getAlgorithmId(username, algorithmName);
    }

    public IEnumerable<RecommendedVideo> GetAlgorithmVideos(string username, string algorithmName, int take)
    {
        var id = _getAlgorithmId(username, algorithmName)!.Value;
        var videoIds = GetAlgorithmVideoIds(id);
        var videoIdsToLoad = GetSubsetOfVideoIds(id, videoIds, take);
        var videos = _videoDbContext.Videos.Where(z => videoIdsToLoad.Contains(z.Id)).ToList();
        return videoIdsToLoad.Select(videoId => TranslateToRecommended(videos.First(z => z.Id == videoId))).ToList();
    }

    private static Dictionary<(string, string), int?> _algorithmNameIdMap = new Dictionary<(string, string), int?>();

    private int? _getAlgorithmId(string username, string algorithmName)
    {
        username = username.ToLower();
        if (_algorithmNameIdMap.TryGetValue((username, algorithmName), out var id))
        {
            return id;
        }
        var foundAlg = _videoDbContext.Algorithms.FirstOrDefault(z => z.Username == username && z.Name == algorithmName);
        var foundId = foundAlg?.Id;
        _algorithmNameIdMap.Add((username, algorithmName), foundId);
        return foundId;
    }

    private List<int> GetAlgorithmVideoIds(int algorithmId)
    {
        var cachedVideoIds = _globalCache.GetAlgorithmVideoIds(algorithmId);
        if (cachedVideoIds != null)
        {
            return cachedVideoIds;
        }
        var videoIds = _videoDbContext.GetAlgorithmVideos(algorithmId, 500);
        var declusteredVideoIds = MergeDecluster(videoIds).Select(z => z.VideoId).ToList();
        _globalCache.SetAlgorithmPosition(algorithmId, declusteredVideoIds);
        return declusteredVideoIds;
    }

    private List<int> GetSubsetOfVideoIds(int algorithmId, List<int> videoIds, int take)
    {
        var position = _ipScopedCache.GetAlgorithmPosition(algorithmId) ?? 0;
        _ipScopedCache.SetAlgorithmPosition(algorithmId, position + take);
        var results = Helpers.GetInfiniteDistinctLoop(videoIds, position).Take(take).ToList();
        return results;
    }
    private IEnumerable<AlgorithmVideoEntity> MergeDecluster(List<AlgorithmVideoEntity> inputVideos)
    {
        var temp = MergeDeclusterRecursive(inputVideos, 0);
        var test = temp.GroupBy(z => z.ChannelId).Select(z => new { z.Key, c = z.Count() }).ToList();
        var test2 = temp.Take(250).GroupBy(z => z.ChannelId).Select(z => new { z.Key, c = z.Count() }).ToList();
        var test3 = temp.Take(125).GroupBy(z => z.ChannelId).Select(z => new { z.Key, c = z.Count() }).ToList();
        var test4 = temp.Take(62).GroupBy(z => z.ChannelId).Select(z => new { z.Key, c = z.Count() }).ToList();
        var list = temp.Select(z => z.ChannelId).ToList();
        return temp;
    }

    private IEnumerable<AlgorithmVideoEntity> MergeDeclusterRecursive(List<AlgorithmVideoEntity> inputVideos, int depth)
    {
        if (inputVideos.Count() <= 2)
        {
            return inputVideos;
        }
        var groups = inputVideos.GroupBy(z => z.ChannelId).ToList();
        var left = new List<AlgorithmVideoEntity>();
        var right = new List<AlgorithmVideoEntity>();
        foreach (var group in groups)
        {
            IEnumerable<AlgorithmVideoEntity> items = Helpers.RandomizeList(group.ToList());
            var percent = group.First().ChannelPercent;//All items in the group have the same channelPercent
            if (depth == 0 && percent > 1)
            {
                //when ChannelPercent is greater than 1, that means that the GetAlgorithmVideos wanted to return more videos than existed on the channel
                //in order for the channel weight to be respected, we will simply duplicate videos from the channel proportional to the excess channelpercent
                //for example, if percent is 1.5, , and items.Count() is 30, we need to add 15 excessItems, since the GetAlgorithmVideos must've wanted 45 videos
                var excessItems = new List<AlgorithmVideoEntity>();
                while (percent >= 2)
                {
                    excessItems.AddRange(items);
                    percent--;
                }
                var itemsToTake = Helpers.RandomRound(items.Count() * (percent - 1));
                excessItems.AddRange(items.Take(itemsToTake));
                items = items.Concat(excessItems);
            }
            var itemList = items.ToList();
            int midIndex = Helpers.RandomRound(itemList.Count / 2.0);
            left.AddRange(itemList.GetRange(0, midIndex));
            right.AddRange(itemList.GetRange(midIndex, itemList.Count - midIndex));
        }
        return MergeDeclusterRecursive(left, depth + 1).Concat(MergeDeclusterRecursive(right, depth + 1));
    }

    private RecommendedVideo TranslateToRecommended(VideoEntity video)
    {
        var thumbnails = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<VideoThumbnail>>(video.ThumbnailsJson)!.ToList();
        return new RecommendedVideo
        {
            Author = video.Author,
            AuthorId = video.AuthorId,
            AuthorUrl = video.AuthorUrl,
            LengthSeconds = video.LengthSeconds,
            Title = video.Title,
            VideoId = video.UniqueId,
            VideoThumbnails = thumbnails,
            ViewCount = video.ViewCount,
            ViewCountText = video.ViewCountText
        };
    }
}
