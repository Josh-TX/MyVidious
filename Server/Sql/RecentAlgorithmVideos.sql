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
        SUM(part2.weight) OVER() AS sum_weight
    FROM part2
),
part4 AS ( --Calculate how many videos to take from each channel/playlist (not rounded yet)
    SELECT 
        part3.channel_id, 
        part3.playlist_id, 
        part3.weight / part3.sum_weight * @p1 AS item_take_decimal,
        FLOOR(part3.weight / part3.sum_weight * @p1) AS item_take_floor
    FROM part3
),
part5 AS ( --use Probabilistic Rounding to round item_take to an int
    SELECT 
        part4.channel_id,
        part4.playlist_id, 
        CASE WHEN RANDOM() < item_take_decimal - item_take_floor 
            THEN item_take_floor + 1 
            ELSE item_take_floor 
        END as item_take
    FROM part4
),
part6 AS ( --limit the channel take based on @p2
    SELECT 
        part5.channel_id,
        part5.playlist_id, 
        part5.item_take,
        CASE WHEN part5.item_take > @p2 THEN @p2 ELSE part5.item_take END AS limited_item_take
    FROM part5
),
part7 AS (
    SELECT 
        part6.channel_id,
        NULL as playlist_id,
        part6.item_take,
        part6.limited_item_take,
        video.Id AS video_id,
        ROW_NUMBER() OVER (PARTITION BY video.channel_id ORDER BY video.estimated_published DESC) AS rn
    FROM part6
    JOIN video
    ON video.channel_id = part6.channel_id
    WHERE part6.item_take > 0

    UNION ALL 

    SELECT 
        NULL as channel_id,
        part6.playlist_id,
        part6.item_take,
        part6.limited_item_take,
        playlist_video.video_id,
        ROW_NUMBER() OVER (PARTITION BY playlist_video.playlist_id ORDER BY video.estimated_published DESC) AS rn
    FROM part6
    JOIN playlist_video
    ON playlist_video.playlist_id = part6.playlist_id
    JOIN video
    ON video.id = playlist_video.video_id
    WHERE part6.item_take > 0
),
part8 AS ( --filter the videos using channel_take
    SELECT 
        part7.channel_id,
        part7.playlist_id,
        part7.video_id,
        ROUND(CAST(part7.item_take / part7.limited_item_take AS numeric), 3) AS in_memory_factor_increase
    FROM part7
    WHERE part7.rn <= part7.limited_item_take
)
SELECT *
FROM part8