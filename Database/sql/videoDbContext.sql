﻿CREATE TABLE algorithm (
    id integer GENERATED BY DEFAULT AS IDENTITY,
    username text NOT NULL,
    name text NOT NULL,
    description text,
    CONSTRAINT pk_algorithm PRIMARY KEY (id)
);


CREATE TABLE algorithm_item_info_entity (
    algorithm_id integer NOT NULL,
    channel_group_id integer,
    channel_id integer,
    weight_multiplier double precision NOT NULL,
    max_channel_weight integer NOT NULL,
    name text NOT NULL,
    video_count integer,
    failure_count integer NOT NULL,
    channel_count integer
);


CREATE TABLE algorithm_video_entity (
    channel_id integer NOT NULL,
    video_id integer NOT NULL,
    in_memory_factor_increase double precision NOT NULL
);


CREATE TABLE channel (
    id integer GENERATED BY DEFAULT AS IDENTITY,
    name text NOT NULL,
    unique_id text NOT NULL,
    handle text,
    description text,
    author_url text,
    thumbnails_json text,
    author_verified boolean NOT NULL,
    auto_generated boolean NOT NULL,
    sub_count integer NOT NULL,
    video_count integer NOT NULL,
    scraped_to_oldest boolean NOT NULL,
    date_last_scraped timestamp with time zone,
    scrape_failure_count smallint NOT NULL,
    CONSTRAINT pk_channel PRIMARY KEY (id)
);


CREATE TABLE channel_group (
    id integer GENERATED BY DEFAULT AS IDENTITY,
    name text NOT NULL,
    description text,
    channel_count integer NOT NULL,
    CONSTRAINT pk_channel_group PRIMARY KEY (id)
);


CREATE TABLE algorithm_item (
    id integer GENERATED BY DEFAULT AS IDENTITY,
    algorithm_id integer NOT NULL,
    channel_group_id integer,
    channel_id integer,
    weight_multiplier double precision NOT NULL,
    max_channel_weight integer NOT NULL,
    CONSTRAINT pk_algorithm_item PRIMARY KEY (id),
    CONSTRAINT fk_algorithm_item_algorithm_algorithm_id FOREIGN KEY (algorithm_id) REFERENCES algorithm (id) ON DELETE CASCADE
);


CREATE TABLE video (
    id integer GENERATED BY DEFAULT AS IDENTITY,
    channel_id integer NOT NULL,
    title text NOT NULL,
    unique_id text NOT NULL,
    author text NOT NULL,
    author_id text NOT NULL,
    author_url text,
    author_verified boolean NOT NULL,
    thumbnails_json text,
    description text,
    view_count bigint NOT NULL,
    length_seconds integer NOT NULL,
    published bigint NOT NULL,
    premiere_timestamp integer,
    live_now boolean NOT NULL,
    premium boolean NOT NULL,
    is_upcoming boolean NOT NULL,
    CONSTRAINT pk_video PRIMARY KEY (id),
    CONSTRAINT fk_video_channel_channel_id FOREIGN KEY (channel_id) REFERENCES channel (id) ON DELETE CASCADE
);


CREATE TABLE channel_group_item (
    channel_group_id integer NOT NULL,
    channel_id integer NOT NULL,
    channel_group_entity_id integer,
    CONSTRAINT pk_channel_group_item PRIMARY KEY (channel_group_id, channel_id),
    CONSTRAINT fk_channel_group_item_channel_channel_id FOREIGN KEY (channel_id) REFERENCES channel (id) ON DELETE CASCADE,
    CONSTRAINT fk_channel_group_item_channel_group_channel_group_entity_id FOREIGN KEY (channel_group_entity_id) REFERENCES channel_group (id)
);


CREATE INDEX ix_algorithm_item_algorithm_id ON algorithm_item (algorithm_id);


CREATE UNIQUE INDEX ix_channel_unique_id ON channel (unique_id);


CREATE INDEX ix_channel_group_item_channel_group_entity_id ON channel_group_item (channel_group_entity_id);


CREATE INDEX ix_channel_group_item_channel_id ON channel_group_item (channel_id);


CREATE INDEX ix_video_channel_id ON video (channel_id);


CREATE UNIQUE INDEX ix_video_unique_id ON video (unique_id);


