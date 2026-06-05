
FROM mongo:8.2.9@sha256:a706cb4e493bcd0262f345b3b0c78732ca0e54301f0d7bbe2b66f26313ce7ccb

ENV ARCHIVE=/home/r4.archive.gz

COPY .docker/linux/r4.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh
