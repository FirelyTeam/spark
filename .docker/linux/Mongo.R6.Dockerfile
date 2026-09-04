FROM mongo:8.3.8@sha256:5211c51171f57ae60842b11664bb244628971b3d35325762a97888337b9bb0db

ENV ARCHIVE=/home/r6.archive.gz

COPY .docker/linux/r6.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh

