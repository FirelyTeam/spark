
FROM mongo:8.2.9@sha256:a706cb4e493bcd0262f345b3b0c78732ca0e54301f0d7bbe2b66f26313ce7ccb

ENV ARCHIVE=/home/stu3.archive.gz

COPY .docker/linux/stu3.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh
