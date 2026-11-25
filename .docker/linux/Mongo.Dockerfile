
FROM mongo:8.3.17@sha256:747773029b84309981a3624a1af64ffc8c4d792283cc0834f26fe01fc6d28b34

COPY .docker/linux/stu3.archive.gz /home/
COPY .docker/linux/mongorestore.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/mongorestore.sh
