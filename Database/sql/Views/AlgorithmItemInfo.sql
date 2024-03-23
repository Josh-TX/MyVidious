CREATE VIEW vw_algorithm_item_Info
AS
SELECT 
	algorithm_item.algorithm_id,
	algorithm_item.max_channel_weight,
	algorithm_item.weight_multiplier,
	algorithm_item.channel_id,
	NULL AS channel_group_id,
	channel.Name || ' ' || channel.Handle AS Name
FROM algorithm_item
JOIN channel 
ON algorithm_item.channel_id = channel.Id
WHERE algorithm_item.channel_id IS NOT NULL

UNION ALL

SELECT 
	algorithm_item.algorithm_id,
	algorithm_item.max_channel_weight,
	algorithm_item.weight_multiplier,
	NULL AS channel_id,
	algorithm_item.channel_group_id,
	channel_group.Name AS Name
FROM algorithm_item
JOIN channel_group 
ON algorithm_item.channel_group_id = channel_group.Id
WHERE algorithm_item.channel_group_id IS NOT NULL