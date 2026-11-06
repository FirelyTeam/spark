
FROM mongo:8.3.13@sha256:e16e606f792ea89d2b4907b54e5e31eb86a7a23e27231dce3a982cc8f2d73cd2

ENV ARCHIVE=/home/stu3.archive.gz

COPY .docker/linux/stu3.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh
