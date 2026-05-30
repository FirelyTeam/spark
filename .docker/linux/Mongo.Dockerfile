
FROM mongo:8.2.9@sha256:d6566e93e6a913cdb622ebe34e0ae7937d50efa60e92363fb4a84404dc890415

COPY .docker/linux/r4.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh
