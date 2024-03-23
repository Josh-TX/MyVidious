#!/bin/bash
set -eou pipefail

pwd

psql -c "CREATE DATABASE invidious;"
# Copied from Invidious' config
psql --username "$POSTGRES_USER" --dbname invidious < /docker-entrypoint-initdb.d/sql/Invidious/channels.sql
psql --username "$POSTGRES_USER" --dbname invidious < /docker-entrypoint-initdb.d/sql/Invidious/videos.sql
psql --username "$POSTGRES_USER" --dbname invidious < /docker-entrypoint-initdb.d/sql/Invidious/channel_videos.sql
psql --username "$POSTGRES_USER" --dbname invidious < /docker-entrypoint-initdb.d/sql/Invidious/users.sql
psql --username "$POSTGRES_USER" --dbname invidious < /docker-entrypoint-initdb.d/sql/Invidious/session_ids.sql
psql --username "$POSTGRES_USER" --dbname invidious < /docker-entrypoint-initdb.d/sql/Invidious/nonces.sql
psql --username "$POSTGRES_USER" --dbname invidious < /docker-entrypoint-initdb.d/sql/Invidious/annotations.sql
psql --username "$POSTGRES_USER" --dbname invidious < /docker-entrypoint-initdb.d/sql/Invidious/playlists.sql
psql --username "$POSTGRES_USER" --dbname invidious < /docker-entrypoint-initdb.d/sql/Invidious/playlist_videos.sql

# Create tables specific to MyVidious
psql -c "CREATE DATABASE myvidious;"
psql --username "$POSTGRES_USER" --dbname myvidious < /docker-entrypoint-initdb.d/sql/videoDbContext.sql
psql --username "$POSTGRES_USER" --dbname myvidious < /docker-entrypoint-initdb.d/sql/identityDbContext.sql
psql --username "$POSTGRES_USER" --dbname myvidious < /docker-entrypoint-initdb.d/sql/Views/AlgorithmItemInfo.sql
psql --username "$POSTGRES_USER" --dbname myvidious < /docker-entrypoint-initdb.d/sql/Views/ChannelVideoCount.sql