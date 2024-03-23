FROM postgres:latest

# Copy initialization scripts into the container
COPY sql/. /docker-entrypoint-initdb.d/sql
COPY init.sh /docker-entrypoint-initdb.d/

# Expose PostgreSQL port
EXPOSE 5432

# Default command to run when the container starts
CMD ["postgres"]