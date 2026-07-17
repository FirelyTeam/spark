FROM mongo:8.2.11@sha256:951c2ff9fc6bdb6cb89b1dfea4a0e8ae3ee4fb287c0bf579b2bba54c7803f75d

ENV ARCHIVE=/home/r4b.archive.gz

COPY .docker/linux/r4b.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh
