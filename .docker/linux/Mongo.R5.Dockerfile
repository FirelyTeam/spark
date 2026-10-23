FROM mongo:8.3.13@sha256:0e900a3364da37b76d642b73b0bba56bd4c6c509ab470cf700436fd866bee3ff

ENV ARCHIVE=/home/r5.archive.gz

COPY .docker/linux/r5.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh

