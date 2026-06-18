
FROM mongo:8.2.11@sha256:49f1d7b87c2ddf918372be5defe7edff8c46703d0b2a56023a3f825e32e1250c

ENV ARCHIVE=/home/r4.archive.gz

COPY .docker/linux/r4.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh
