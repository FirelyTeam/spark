
FROM mongo:8.2.7@sha256:9b18c8c1470a5aebb150ce87d6386f78f29ce74b96dfe55524161065eb8d71db

COPY .docker/linux/r4.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh
