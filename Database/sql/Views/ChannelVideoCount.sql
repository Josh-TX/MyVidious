CREATE VIEW vw_channel_video_count
AS
SELECT 
	channel.Id AS channel_id, 
	channel.unique_id,
	COUNT(*) AS video_count
FROM channel
JOIN video
ON video.channel_id = channel.Id
WHERE scraped_to_oldest = true
GROUP BY channel.Id, channel.unique_id
UNION ALL
SELECT
	channel.Id AS channel_id, 
	channel.unique_id,
	NULL AS video_count
FROM channel
WHERE scraped_to_oldest = false

