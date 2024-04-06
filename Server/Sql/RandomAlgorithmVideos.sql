WITH part1 AS ( --Get all channel_ids or playlist_ids for the algorithm_Id
    SELECT 
        channel_id,
        playlist_id,
        algorithm.max_item_weight, 
        algorithm_item.weight_multiplier,
        CASE WHEN channel.id IS NOT NULL 
            THEN channel.video_count 
            ELSE playlist.video_count
        END AS video_count
    FROM algorithm_item
    JOIN algorithm
    ON algorithm.id = algorithm_item.algorithm_id
    LEFT JOIN channel
    ON channel.id = algorithm_item.channel_id
    LEFT JOIN playlist
    ON playlist.id = algorithm_item.playlist_id
    WHERE algorithm_Id = @p0
),
part2 AS ( --Calculate the items's weight
    SELECT 
        part1.channel_id,
        part1.playlist_id,
        part1.video_count,
        CASE WHEN part1.video_count > part1.max_item_weight 
            THEN part1.max_item_weight * part1.weight_multiplier 
            ELSE part1.video_count * part1.weight_multiplier 
        END AS weight
    FROM part1
),
part3 AS ( --Calculate sum weight of all channels/playlists for the algorithm
    SELECT 
        part2.channel_id, 
        part2.playlist_id,
        part2.weight,
        part2.video_count,
        SUM(part2.weight) OVER() AS sum_weight
    FROM part2
),
part4 AS ( --Calculate how many videos to take from each channel/playlist (not rounded yet)
    SELECT 
        part3.channel_id, 
        part3.playlist_id, 
        part3.video_count,
        part3.weight / part3.sum_weight * @p1 AS item_take_decimal,
        FLOOR(part3.weight / part3.sum_weight * @p1) AS item_take_floor
    FROM part3
),
part5 AS ( --use Probabilistic Rounding to round item_take to an int
    SELECT 
        part4.channel_id,
        part4.playlist_id, 
        part4.video_count,
        CASE WHEN RANDOM() < item_take_decimal - item_take_floor 
            THEN item_take_floor + 1 
            ELSE item_take_floor 
        END as item_take
    FROM part4
),
part6 AS (
    SELECT 
        part5.channel_id,
        NULL as playlist_id,
        part5.item_take,
        part5.video_count,
        video.Id AS video_id,
        ROW_NUMBER() OVER (PARTITION BY video.channel_id ORDER BY RANDOM()) AS rn
    FROM part5
    JOIN video
    ON video.channel_id = part5.channel_id
    WHERE part5.item_take > 0

    UNION ALL 

    SELECT 
        NULL as channel_id,
        part5.playlist_id,
        part5.item_take,
        part5.video_count,
        playlist_video.video_id,
        ROW_NUMBER() OVER (PARTITION BY playlist_video.playlist_id ORDER BY RANDOM()) AS rn
    FROM part5
    JOIN playlist_video
    ON playlist_video.playlist_id = part5.playlist_id
    WHERE part5.item_take > 0
),
part7 AS ( --filter the videos using channel_take
    SELECT 
        part6.channel_id,
        part6.playlist_id,
        part6.video_id,
        ROUND(CAST(part6.item_take / part6.video_count AS numeric), 3) AS in_memory_factor_increase
    FROM part6
    WHERE part6.rn <= part6.item_take
)
SELECT *
FROM part7