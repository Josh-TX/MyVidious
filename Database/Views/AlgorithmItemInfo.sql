CREATE VIEW vw_AlgorithmItemInfo
AS
SELECT 
	AlgorithmItem.AlgorithmId,
	AlgorithmItem.MaxChannelWeight,
	AlgorithmItem.WeightMultiplier,
	AlgorithmItem.ChannelId,
	NULL AS ChannelGroupId,
	Channel.Name + ' ' + channel.Handle AS Name
FROM AlgorithmItem
JOIN Channel 
ON AlgorithmItem.ChannelId = Channel.Id
WHERE AlgorithmItem.ChannelId IS NOT NULL

UNION ALL

SELECT 
	AlgorithmItem.AlgorithmId,
	AlgorithmItem.MaxChannelWeight,
	AlgorithmItem.WeightMultiplier,
	NULL AS ChannelId,
	AlgorithmItem.ChannelGroupId,
	ChannelGroup.Name AS Name
FROM AlgorithmItem
JOIN ChannelGroup 
ON AlgorithmItem.ChannelGroupId = ChannelGroup.Id
WHERE AlgorithmItem.ChannelGroupId IS NOT NULL