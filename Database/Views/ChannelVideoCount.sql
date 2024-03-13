CREATE VIEW vw_ChannelVideoCount
AS
SELECT 
	Channel.Id AS ChannelId, 
	Channel.UniqueId,
	COUNT(*) AS VideoCount
FROM Channel
JOIN Video
ON Video.ChannelId = Channel.Id
WHERE ScrapedToOldest = 1
GROUP BY Channel.Id, Channel.UniqueId
UNION ALL
SELECT
	Channel.Id AS ChannelId, 
	Channel.UniqueId,
	NULL AS VideoCount
FROM Channel
WHERE ScrapedToOldest = 0

