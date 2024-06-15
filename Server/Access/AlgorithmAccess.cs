using MyVidious.Data;
using MyVidious.Models;
using MyVidious.Models.Invidious;
using MyVidious.Utilities;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

    public ChannelAndPlaylistIds GetChannelAndPlaylistIds(string username, string algorithmName)
    {
        var id = _getAlgorithmId(username, algorithmName)!.Value;
        var items = _videoDbContext.AlgorithmItems.Where(z => z.AlgorithmId == id).ToList();
        return new ChannelAndPlaylistIds
        {
            ChannelIds = items.Where(z => z.ChannelId.HasValue).Select(z => z.ChannelId!.Value),
            PlaylistIds = items.Where(z => z.PlaylistId.HasValue).Select(z => z.PlaylistId!.Value),
        };
    }

    public bool ShouldBiasRecommendations(string username, string algorithmName)
    {
        if (_algorithmNameBiasMap.TryGetValue((username.ToLower(), algorithmName.ToLower()), out var shouldBias))
        {
            return shouldBias;
        }
        var foundAlg = _videoDbContext.Algorithms.FirstOrDefault(z => z.Username.ToLower() == username.ToLower() && z.Name.ToLower() == algorithmName.ToLower());
        var bias = foundAlg?.BiasCurrentChannel;
        if (bias.HasValue)
        {
            _algorithmNameBiasMap.TryAdd((username.ToLower(), algorithmName.ToLower()), bias.Value);
        }
        return true;
    }

    public void BustAlgorithmCache(string username, string algorithmName)
    {
        //this is important because if an algorithm was deleted, and then another one was created with same username & algorithmName, it'd map to the wrong Id
        _algorithmNameIdMap.TryRemove((username.ToLower(), algorithmName.ToLower()), out _);
        _algorithmNameBiasMap.TryRemove((username.ToLower(), algorithmName.ToLower()), out _);
    }


    private static ConcurrentDictionary<(string, string), int> _algorithmNameIdMap = new ConcurrentDictionary<(string, string), int>();
    private static ConcurrentDictionary<(string, string), bool> _algorithmNameBiasMap = new ConcurrentDictionary<(string, string), bool>();

    private int? _getAlgorithmId(string username, string algorithmName)
    {
        if (_algorithmNameIdMap.TryGetValue((username.ToLower(), algorithmName.ToLower()), out var id))
        {
            return id;
        }
        var foundAlg = _videoDbContext.Algorithms.FirstOrDefault(z => z.Username.ToLower() == username.ToLower() && z.Name.ToLower() == algorithmName.ToLower());
        var foundId = foundAlg?.Id;
        if (foundId.HasValue)
        {
            _algorithmNameIdMap.TryAdd((username.ToLower(), algorithmName.ToLower()), foundId.Value);
        }
        return foundId;
    }

    private List<int> GetRandomAlgorithmVideoIds(int algorithmId)
    {
        var cachedVideoIds = _globalCache.GetRandomAlgorithmVideoIds(algorithmId);
        if (cachedVideoIds != null)
        {
            return cachedVideoIds;
        }
        var algorithmItems = _videoDbContext.GetRandomAlgorithmVideos(algorithmId, 500);
        algorithmItems = ApplyFactorIncrease(algorithmItems);
        var declusteredVideoIds = algorithmItems.Decluster(z => new { z.ChannelId, z.PlaylistId }).Select(z => z.VideoId).ToList();
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
        var algorithmItems = _videoDbContext.GetRecentAlgorithmVideos(algorithmId, 500);
        algorithmItems = ApplyFactorIncrease(algorithmItems);
        var declusteredVideoIds = algorithmItems.Decluster(z => new { z.ChannelId, z.PlaylistId }).Select(z => z.VideoId).ToList();
        _globalCache.SetRecentAlgorithmVideoIds(algorithmId, declusteredVideoIds);
        return declusteredVideoIds;
    }

    private List<AlgorithmVideoEntity> ApplyFactorIncrease(List<AlgorithmVideoEntity> algorithmItems)
    {
        var results = new List<AlgorithmVideoEntity>(); 
        var groups = algorithmItems.GroupBy(z => new { z.ChannelId, z.PlaylistId });
        var randGroups = Helpers.RandomizeList(groups.ToList());
        foreach (var group in randGroups)
        {
            IEnumerable<AlgorithmVideoEntity> items = Helpers.RandomizeList(group.ToList());
            var factorIncrease = group.First().InMemoryFactorIncrease;//All items in the group have the same channelPercent
            if (factorIncrease > 1)
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
            results.AddRange(items);
        }
        return results;
    }


    private List<int> GetNextVideoIds(int algorithmId, List<int> videoIds, int take)
    {
        var position = _ipScopedCache.GetAlgorithmPosition(algorithmId) ?? 0;
        _ipScopedCache.SetAlgorithmPosition(algorithmId, position + take);
        var nextVideoIds = Helpers.GetInfiniteDistinctLoop(videoIds, position).Take(take).ToList();
        return nextVideoIds;
    }

    public RecommendedVideo TranslateToRecommended(VideoEntity video)
    {
        var thumbnails = video.ThumbnailsJson != null 
            ? System.Text.Json.JsonSerializer.Deserialize<IEnumerable<VideoThumbnail>>(video.ThumbnailsJson)!.ToList()
            : Enumerable.Empty<VideoThumbnail>();
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
            ViewCountText = Helpers.FormatViews(video.ViewCount)
        };
    }

    private VideoObject TranslateToVideoObject(VideoEntity video)
    {
        var thumbnails = video.ThumbnailsJson != null
            ? System.Text.Json.JsonSerializer.Deserialize<IEnumerable<VideoThumbnail>>(video.ThumbnailsJson)!.ToList()
            : new List<VideoThumbnail>();
        thumbnails = thumbnails.Select(_imageUrlUtility.FixImageUrl).ToList();
        var published = video.ActualPublished ?? video.EstimatedPublished ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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
            DescriptionHtml = System.Web.HttpUtility.HtmlEncode(video.Description),

            LengthSeconds = video.LengthSeconds,
            ViewCount = video.ViewCount,
            ViewCountText = Helpers.FormatViews(video.ViewCount),

            Published = published,
            PublishedText = Helpers.GetPublishedText(published),
            PremiereTimestamp = video.PremiereTimestamp,
            LiveNow = video.LiveNow,
            Premium = video.Premium,
            IsUpcoming = video.IsUpcoming
        };
    }

    private PopularVideo TranslateToPopularVideo(VideoEntity video)
    {
        var thumbnails = video.ThumbnailsJson != null 
            ? System.Text.Json.JsonSerializer.Deserialize<IEnumerable<VideoThumbnail>>(video.ThumbnailsJson)!.ToList()
            : new List<VideoThumbnail>();
        thumbnails = thumbnails.Select(_imageUrlUtility.FixImageUrl).ToList();
        var published = video.ActualPublished ?? video.EstimatedPublished ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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

            Published = published,
            PublishedText = Helpers.GetPublishedText(published),
        };
    }
}
