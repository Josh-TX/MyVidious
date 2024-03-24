SELECT 
	algorithm_item.algorithm_id,
	algorithm_item.max_channel_weight,
	algorithm_item.weight_multiplier,
	algorithm_item.channel_id,
	NULL AS channel_group_id,
	channel.name,
	channel.video_count,
	channel.scrape_failure_count AS failure_count,
	NULL AS channel_count
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
	channel_group.Name AS Name,
	NULL AS video_count,
	0 AS failure_count,
	channel_group.channel_count
FROM algorithm_item
JOIN channel_group 
ON algorithm_item.channel_group_id = channel_group.Id
WHERE algorithm_item.channel_group_id IS NOT NULL