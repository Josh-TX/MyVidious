#!/bin/bash
set -eou pipefail

pwd

echo Setting up Invidious Database
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

echo Setting up Invidious Database
# Create tables specific to MyVidious
psql -c "CREATE DATABASE myvidious;"
echo Running videoDbContext.sql
psql --username "$POSTGRES_USER" --dbname myvidious < /docker-entrypoint-initdb.d/sql/videoDbContext.sql
echo Running identityDbContext.sql
psql --username "$POSTGRES_USER" --dbname myvidious < /docker-entrypoint-initdb.d/sql/identityDbContext.sql
echo Running postdeploy.sql
psql --username "$POSTGRES_USER" --dbname myvidious < /docker-entrypoint-initdb.d/sql/postdeploy.sql