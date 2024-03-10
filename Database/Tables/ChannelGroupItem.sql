CREATE TABLE ChannelGroupItem (
	ChannelGroupId INT NOT NULL FOREIGN KEY REFERENCES ChannelGroup(Id),
	ChannelId INT NOT NULL FOREIGN KEY REFERENCES Channel(Id),
	CONSTRAINT PK_ChannelGroupItem PRIMARY KEY (ChannelGroupId,ChannelId)
)