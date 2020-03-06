FROM mongo
COPY .docker/linux/dstu2.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/
