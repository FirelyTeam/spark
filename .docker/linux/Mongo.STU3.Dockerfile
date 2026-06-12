
FROM mongo:8.2.10@sha256:1286be0f98b0da2575280a7a07e50446dfd707d683fd8e51937526b6e3c65fd9

ENV ARCHIVE=/home/stu3.archive.gz

COPY .docker/linux/stu3.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh
