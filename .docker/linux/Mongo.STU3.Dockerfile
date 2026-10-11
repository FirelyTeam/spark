
FROM mongo:8.3.11@sha256:d731d77bfd7afd66bd487bdf627b5bf7ce4c3602ec461d635977021db529ebbc

ENV ARCHIVE=/home/stu3.archive.gz

COPY .docker/linux/stu3.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh
