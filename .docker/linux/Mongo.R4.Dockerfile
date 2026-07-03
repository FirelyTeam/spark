
FROM mongo:8.2.11@sha256:b8806ee8207318a30316eca72257da4c146025a80fdcdb4c597e596af9233ee3

ENV ARCHIVE=/home/r4.archive.gz

COPY .docker/linux/r4.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh
