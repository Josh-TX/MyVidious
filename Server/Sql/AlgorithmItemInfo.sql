SELECT 
	algorithm_item.algorithm_id,
	algorithm.max_item_weight,
	algorithm_item.weight_multiplier,
	algorithm_item.folder,
	algorithm_item.channel_id,
	NULL AS playlist_id,
	channel.name,
	channel.video_count,
	channel.unique_id,
	channel.scrape_failure_count AS failure_count
FROM algorithm_item
JOIN channel
ON algorithm_item.channel_id = channel.Id
JOIN algorithm
ON algorithm.id = algorithm_item.algorithm_id
WHERE algorithm_item.channel_id IS NOT NULL

UNION ALL

SELECT 
	algorithm_item.algorithm_id,
	algorithm.max_item_weight,
	algorithm_item.weight_multiplier,
	algorithm_item.folder,
	NULL AS channel_id,
	algorithm_item.playlist_id,
	playlist.Title AS name,
	playlist.video_count,
	playlist.unique_id,
	playlist.scrape_failure_count AS failure_count
FROM algorithm_item
JOIN playlist 
ON algorithm_item.playlist_id = playlist.Id
JOIN algorithm
ON algorithm.id = algorithm_item.algorithm_id
WHERE algorithm_item.playlist_id IS NOT NULL