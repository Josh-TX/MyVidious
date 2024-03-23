﻿CREATE TABLE "Algorithm" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Username" text NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    CONSTRAINT "PK_Algorithm" PRIMARY KEY ("Id")
);


CREATE TABLE "Channel" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" text NOT NULL,
    "UniqueId" text NOT NULL,
    "Handle" text NOT NULL,
    "ScrapedToOldest" boolean NOT NULL,
    "DateLastScraped" timestamp with time zone,
    "ScrapeFailureCount" smallint NOT NULL,
    CONSTRAINT "PK_Channel" PRIMARY KEY ("Id")
);


CREATE TABLE "AlgorithmItem" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "AlgorithmId" integer NOT NULL,
    "ChannelGroupId" integer,
    "ChannelId" integer,
    "WeightMultiplier" double precision NOT NULL,
    "MaxChannelWeight" integer NOT NULL,
    CONSTRAINT "PK_AlgorithmItem" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AlgorithmItem_Algorithm_AlgorithmId" FOREIGN KEY ("AlgorithmId") REFERENCES "Algorithm" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Video" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "ChannelId" integer NOT NULL,
    "Title" text NOT NULL,
    "UniqueId" text NOT NULL,
    "Author" text NOT NULL,
    "AuthorId" text NOT NULL,
    "AuthorUrl" text NOT NULL,
    "AuthorVerified" boolean NOT NULL,
    "ThumbnailsJson" text NOT NULL,
    "Description" text NOT NULL,
    "DescriptionHtml" text NOT NULL,
    "ViewCount" bigint NOT NULL,
    "ViewCountText" text NOT NULL,
    "LengthSeconds" integer NOT NULL,
    "Published" bigint NOT NULL,
    "PublishedText" text NOT NULL,
    "PremiereTimestamp" integer,
    "LiveNow" boolean NOT NULL,
    "Premium" boolean NOT NULL,
    "IsUpcoming" boolean NOT NULL,
    CONSTRAINT "PK_Video" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Video_Channel_ChannelId" FOREIGN KEY ("ChannelId") REFERENCES "Channel" ("Id") ON DELETE CASCADE
);


CREATE INDEX "IX_AlgorithmItem_AlgorithmId" ON "AlgorithmItem" ("AlgorithmId");


CREATE INDEX "IX_Video_ChannelId" ON "Video" ("ChannelId")


