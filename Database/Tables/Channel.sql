﻿CREATE TABLE Channel (
	Id INT PRIMARY KEY IDENTITY(1,1),
	Name VARCHAR(100) NOT NULL,
	UniqueId VARCHAR(40) NOT NULL,
	Handle VARCHAR(100) NULL,

	ScrapedToOldest BIT NOT NULL,
	DateLastScraped DATETIME NULL,
	ScrapeFailureCount SMALLINT NOT NULL DEFAULT 0,

	InsertDate DATETIME NOT NULL DEFAULT GETDATE()
)