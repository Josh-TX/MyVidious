CREATE PROCEDURE GetAlgorithmVideos
    @AlgorithmId INT,
    @Take INT
AS
BEGIN

    WITH part1 AS (
        SELECT ChannelId, AlgorithmItem.MaxChannelWeight, AlgorithmItem.WeightMultiplier
        FROM AlgorithmItem
        WHERE AlgorithmId = @AlgorithmId
        AND ChannelId IS NOT NULL

        UNION ALL

        SELECT ChannelGroupItem.ChannelId, AlgorithmItem.MaxChannelWeight, AlgorithmItem.WeightMultiplier
        FROM AlgorithmItem
        JOIN ChannelGroupItem
        ON ChannelGroupItem.ChannelGroupId = AlgorithmItem.ChannelGroupId
        WHERE AlgorithmId = @AlgorithmId
    ),
    part2 AS (
        SELECT 
            part1.*, COUNT(*) AS ChannelVideoCount
        FROM part1
        JOIN Video
        ON Video.ChannelId = part1.ChannelId
        GROUP BY part1.ChannelId, part1.MaxChannelWeight, part1.WeightMultiplier
    ),
    part3 AS (
        SELECT 
            part2.ChannelId,
            part2.ChannelVideoCount,
            CASE WHEN part2.ChannelVideoCount > part2.MaxChannelWeight THEN part2.MaxChannelWeight * part2.WeightMultiplier ELSE part2.ChannelVideoCount * part2.WeightMultiplier END AS Weight
        FROM part2
    ),
    part4 AS (
        SELECT 
            part3.ChannelId, 
            part3.Weight,
            part3.ChannelVideoCount,
            SUM(part3.Weight) OVER() AS sumWeight
        FROM part3
    ),
    part5 AS (
        SELECT 
            part4.ChannelId, 
            part4.ChannelVideoCount,
            part4.Weight / part4.sumWeight * @Take AS channelTakeDecimal,
            FLOOR(part4.Weight / part4.sumWeight * @Take) AS channelTakeFloor
        FROM part4
    ),
    part6 AS (
        SELECT 
            part5.ChannelId,
            part5.ChannelVideoCount,
            --for performance reasons, I'm fine with using RAND() instead of RAND(CHECKSUM(NEWID()))
            --The only downside is that @Take() will be statistically less accurate, but @Take() is already inaccurate when channelTake exceeds the # of videos on the channel
            CASE WHEN RAND() < channelTakeDecimal - channelTakeFloor THEN channelTakeFloor + 1 ELSE channelTakeFloor END as channelTake
        FROM part5
    ),
    part7 AS (
        SELECT 
            part6.ChannelId,
            part6.ChannelTake,
            part6.ChannelVideoCount,
            Video.Id AS VideoId,
            ROW_NUMBER() OVER (PARTITION BY Video.ChannelId ORDER BY Video.Id) AS rn
        FROM part6
        JOIN Video
        ON Video.ChannelId = part6.ChannelId
    ),
    part8 AS (
        SELECT 
            part7.ChannelId,
            part7.VideoId,
            ROUND(part7.channelTake / part7.ChannelVideoCount, 3) AS ChannelPercent
        FROM part7
        WHERE part7.rn <= part7.channelTake
    )
    SELECT *
    FROM part8
END