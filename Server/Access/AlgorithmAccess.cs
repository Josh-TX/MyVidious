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
    private ImageUrlUtility _imageUrlUtility;

    public AlgorithmAccess(
        VideoDbContext videoDbContext, 
        GlobalCache globalCache, 
        IPScopedCache ipScopedCache,
        ImageUrlUtility imageUrlUtility

        )
    {
        _videoDbContext = videoDbContext;
        _globalCache = globalCache;
        _ipScopedCache = ipScopedCache;
        _imageUrlUtility = imageUrlUtility;
    }

    public int? GetAlgorithmId(string username, string algorithmName)
    {
        return _getAlgorithmId(username, algorithmName);
    }

    public IEnumerable<PopularVideo> GetPopularAlgorithmVideos(string username, string algorithmName, int take)
    {
        var id = _getAlgorithmId(username, algorithmName)!.Value;
        var videoIds = GetRecentAlgorithmVideoIds(id);
        var videoIdsToLoad = GetNextVideoIds(id, videoIds, take);
        var videos = _videoDbContext.Videos.Where(z => videoIdsToLoad.Contains(z.Id)).ToList();
        //since nextVideoIds has been explicitly declustered, we want it sorted by nextVideoIds
        return videoIdsToLoad.Select(videoId => TranslateToPopularVideo(videos.First(z => z.Id == videoId))).ToList();
    }

    public IEnumerable<RecommendedVideo> GetRandomAlgorithmVideos(string username, string algorithmName, int take)
    {
        var id = _getAlgorithmId(username, algorithmName)!.Value;
        var videoIds = GetRandomAlgorithmVideoIds(id);
        var videoIdsToLoad = GetNextVideoIds(id, videoIds, take);
        var videos = _videoDbContext.Videos.Where(z => videoIdsToLoad.Contains(z.Id)).ToList();
        //since nextVideoIds has been explicitly declustered, we want it sorted by nextVideoIds
        return videoIdsToLoad.Select(videoId => TranslateToRecommended(videos.First(z => z.Id == videoId))).ToList();
    }

    public IEnumerable<VideoObject> GetTrendingAlgorithmVideos(string username, string algorithmName, int take)
    {
        var id = _getAlgorithmId(username, algorithmName)!.Value;
        var videoIds = GetRandomAlgorithmVideoIds(id);
        //Iterate backwards from the end so that trending will be as different as possible from recommended videos
        var nextVideoIds = Helpers.GetBackwardsInfiniteDistinctLoop(videoIds, 0).Take(take).ToList();
        var videos = _videoDbContext.Videos.Where(z => nextVideoIds.Contains(z.Id)).ToList();
        //since nextVideoIds has been explicitly declustered, we want it sorted by nextVideoIds
        return nextVideoIds.Select(videoId => TranslateToVideoObject(videos.First(z => z.Id == videoId))).ToList();
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

    private List<int> GetRandomAlgorithmVideoIds(int algorithmId)
    {
        var cachedVideoIds = _globalCache.GetRandomAlgorithmVideoIds(algorithmId);
        if (cachedVideoIds != null)
        {
            return cachedVideoIds;
        }
        var videoIds = _videoDbContext.GetRandomAlgorithmVideos(algorithmId, 500);
        var declusteredVideoIds = MergeDecluster(videoIds).Select(z => z.VideoId).ToList();
        _globalCache.SetRandomAlgorithmVideoIds(algorithmId, declusteredVideoIds);
        return declusteredVideoIds;
    }

    private List<int> GetRecentAlgorithmVideoIds(int algorithmId)
    {
        var cachedVideoIds = _globalCache.GetRecentAlgorithmVideoIds(algorithmId);
        if (cachedVideoIds != null)
        {
            return cachedVideoIds;
        }
        var videoIds = _videoDbContext.GetRecentAlgorithmVideos(algorithmId, 500);
        var declusteredVideoIds = MergeDecluster(videoIds).Select(z => z.VideoId).ToList();
        _globalCache.SetRecentAlgorithmVideoIds(algorithmId, declusteredVideoIds);
        return declusteredVideoIds;
    }

    private List<int> GetNextVideoIds(int algorithmId, List<int> videoIds, int take)
    {
        var position = _ipScopedCache.GetAlgorithmPosition(algorithmId) ?? 0;
        _ipScopedCache.SetAlgorithmPosition(algorithmId, position + take);
        var nextVideoIds = Helpers.GetInfiniteDistinctLoop(videoIds, position).Take(take).ToList();
        return nextVideoIds;
    }

    private IEnumerable<AlgorithmVideoEntity> MergeDecluster(List<AlgorithmVideoEntity> inputVideos)
    {
        return MergeDeclusterRecursive(inputVideos, 0); ;
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
            var factorIncrease = group.First().InMemoryFactorIncrease;//All items in the group have the same channelPercent
            if (depth == 0 && factorIncrease > 1)
            {
                //when InMemoryFactorIncrease is greater than 1, that means that the algorithm proc wanted to return more videos, but was limited by something
                //in order for InMemoryFactorIncrease to be respected, we will simply duplicate videos from the channel proportional to the InMemoryFactorIncrease
                //for example, if InMemoryFactorIncrease is 1.5, and items.Count() is 30, we need to add 15 excessItems
                var excessItems = new List<AlgorithmVideoEntity>();
                while (factorIncrease >= 2)
                {
                    excessItems.AddRange(items);
                    factorIncrease--;
                }
                var itemsToTake = Helpers.RandomRound(items.Count() * (factorIncrease - 1));
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
        thumbnails = thumbnails.Select(_imageUrlUtility.FixImageUrl).ToList();
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

    private VideoObject TranslateToVideoObject(VideoEntity video)
    {
        var thumbnails = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<VideoThumbnail>>(video.ThumbnailsJson)!.ToList();
        thumbnails = thumbnails.Select(_imageUrlUtility.FixImageUrl).ToList();
        return new VideoObject
        {
            Type = "video",
            Title = video.Title,
            VideoId = video.UniqueId,

            Author = video.Author,
            AuthorId = video.AuthorId,
            AuthorUrl = video.AuthorUrl,
            AuthorVerified = video.AuthorVerified,

            VideoThumbnails = thumbnails,
            Description = video.Description,
            DescriptionHtml = video.DescriptionHtml,

            LengthSeconds = video.LengthSeconds,
            ViewCount = video.ViewCount,
            ViewCountText = video.ViewCountText,

            Published = video.Published,
            PublishedText = video.PublishedText,
            PremiereTimestamp = video.PremiereTimestamp,
            LiveNow = video.LiveNow,
            Premium = video.Premium,
            IsUpcoming = video.IsUpcoming
        };
    }

    private PopularVideo TranslateToPopularVideo(VideoEntity video)
    {
        var thumbnails = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<VideoThumbnail>>(video.ThumbnailsJson)!.ToList();
        thumbnails = thumbnails.Select(_imageUrlUtility.FixImageUrl).ToList();
        return new PopularVideo
        {
            Type = "shortVideo",
            Title = video.Title,

            VideoId = video.UniqueId,
            VideoThumbnails = thumbnails,

            ViewCount = video.ViewCount,
            LengthSeconds = video.LengthSeconds,

            Author = video.Author,
            AuthorId = video.AuthorId,
            AuthorUrl = video.AuthorUrl,

            Published = video.Published,
            PublishedText = video.PublishedText,
        };
    }
}
