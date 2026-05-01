
FROM mongo:8.2.7

ENV ARCHIVE=/home/stu3.archive.gz

COPY .docker/linux/stu3.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh
