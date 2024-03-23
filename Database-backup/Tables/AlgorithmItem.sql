CREATE TABLE AlgorithmItem (
	Id INT PRIMARY KEY IDENTITY(1,1),
	AlgorithmId INT NOT NULL,
	ChannelGroupId INT NULL FOREIGN KEY REFERENCES ChannelGroup(Id),
	ChannelId INT NULL FOREIGN KEY REFERENCES Channel(Id),
	WeightMultiplier FLOAT NOT NULL,
	MaxChannelWeight INT NOT NULL,
)