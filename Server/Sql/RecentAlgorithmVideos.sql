WITH part1 AS ( --Get all channel_ids for the algorithm_Id
    SELECT 
        channel_id,
        algorithm.max_item_weight, 
        algorithm_item.weight_multiplier,
        channel.video_count AS channel_video_count
    FROM algorithm_item
    JOIN algorithm
    ON algorithm.id = algorithm_item.algorithm_id
    JOIN channel
    ON channel.id = algorithm_item.channel_id
    WHERE algorithm_Id = @p0
),
part4 AS ( --Calculate the channel's weight
    SELECT 
        part1.channel_id,
        part1.channel_video_count,
        CASE WHEN part1.channel_video_count > part1.max_item_weight THEN part1.max_item_weight * part1.weight_multiplier ELSE part1.channel_video_count * part1.weight_multiplier END AS weight
    FROM part1
),
part5 AS ( --Calculate sum weight of all channels on the algorithm
    SELECT 
        part4.channel_id, 
        part4.weight,
        SUM(part4.weight) OVER() AS sum_weight
    FROM part4
),
part6 AS ( --Calculate how many videos to take from each channel (not rounded yet)
    SELECT 
        part5.channel_id, 
        part5.weight / part5.sum_weight * @p1 AS channel_take_decimal,
        FLOOR(part5.weight / part5.sum_weight * @p1) AS channel_take_floor
    FROM part5
),
part7 AS ( --use Probabilistic Rounding to round channel_take to an int
    SELECT 
        part6.channel_id,
        CASE WHEN RANDOM() < channel_take_decimal - channel_take_floor THEN channel_take_floor + 1 ELSE channel_take_floor END as channel_take
    FROM part6
),
part8 AS ( --limit the channel take based on @p2
    SELECT 
        part7.channel_id,
        part7.channel_take,
        CASE WHEN part7.channel_take > @p2 THEN @p2 ELSE part7.channel_take END AS limited_channel_take
    FROM part7
),
part9 AS ( --finally join with videos. Up until this point the each query row corresponded to a channel
    SELECT 
        part8.channel_id,
        part8.channel_take,
        part8.limited_channel_take,
        video.Id AS video_id,
        ROW_NUMBER() OVER (PARTITION BY video.channel_id ORDER BY video.published DESC) AS rn
    FROM part8
    JOIN video
    ON video.channel_id = part8.channel_id
    WHERE part8.channel_take > 0
),
part10 AS ( --filter the videos using channel_take
    SELECT 
        part9.channel_id,
        part9.video_id,
        ROUND(CAST(part9.channel_take / part9.limited_channel_take AS numeric), 3) AS in_memory_factor_increase
    FROM part9
    WHERE part9.rn <= part9.limited_channel_take
)
SELECT *
FROM part10