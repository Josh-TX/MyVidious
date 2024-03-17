CREATE PROCEDURE GetRandomAlgorithmVideos
    @AlgorithmId INT,
    @Take INT
AS
BEGIN
    WITH part1 AS ( --Get all ChannelIds for the AlgorithmId
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
    part2 AS ( --combine duplicate channels
        SELECT 
            part1.ChannelId, 
            AVG(part1.MaxChannelWeight) AS MaxChannelWeight, 
            SUM(part1.WeightMultiplier) AS WeightMultiplier
        FROM part1
        GROUP BY part1.ChannelId
        HAVING SUM(part1.WeightMultiplier) > 0
    ),
    part3 AS ( --Get the ChannelVideoCount of each Channel
        SELECT 
            part2.*, 
            COUNT(*) AS ChannelVideoCount
        FROM part2
        JOIN Video
        ON Video.ChannelId = part2.ChannelId
        GROUP BY part2.ChannelId, part2.MaxChannelWeight, part2.WeightMultiplier
    ),
    part4 AS ( --Calculate the channel's weight
        SELECT 
            part3.ChannelId,
            part3.ChannelVideoCount,
            CASE WHEN part3.ChannelVideoCount > part3.MaxChannelWeight THEN part3.MaxChannelWeight * part3.WeightMultiplier ELSE part3.ChannelVideoCount * part3.WeightMultiplier END AS Weight
        FROM part3
    ),
    part5 AS ( --Calculate sum weight of all channels on the algorithm
        SELECT 
            part4.ChannelId, 
            part4.Weight,
            part4.ChannelVideoCount,
            SUM(part4.Weight) OVER() AS sumWeight
        FROM part4
    ),
    part6 AS ( --Calculate how many videos to take from each channel (not rounded yet)
        SELECT 
            part5.ChannelId, 
            part5.ChannelVideoCount,
            part5.Weight / part5.sumWeight * @Take AS channelTakeDecimal,
            FLOOR(part5.Weight / part5.sumWeight * @Take) AS channelTakeFloor
        FROM part5
    ),
    part7 AS ( --use Probabilistic Rounding to round channelTake to an int
        SELECT 
            part6.ChannelId,
            part6.ChannelVideoCount,
            CASE WHEN RAND(CHECKSUM(NEWID())) < channelTakeDecimal - channelTakeFloor THEN channelTakeFloor + 1 ELSE channelTakeFloor END as channelTake
        FROM part6
    ),
    part8 AS ( --finally join with Videos. Up until this point the each query row corresponded to a channel
        SELECT 
            part7.ChannelId,
            part7.ChannelTake,
            part7.ChannelVideoCount,
            Video.Id AS VideoId,
            ROW_NUMBER() OVER (PARTITION BY Video.ChannelId ORDER BY NEWID()) AS rn
        FROM part7
        JOIN Video
        ON Video.ChannelId = part7.ChannelId
        WHERE part7.ChannelTake > 0
    ),
    part9 AS ( --filter the videos using channelTake
        SELECT 
            part8.ChannelId,
            part8.VideoId,
            ROUND(part8.channelTake / part8.ChannelVideoCount, 3) AS InMemoryFactorIncrease
        FROM part8
        WHERE part8.rn <= part8.channelTake
    )
    SELECT *
    FROM part9
END