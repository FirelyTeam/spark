#!/bin/bash

ARCHIVE=${1:/home/r4.archive.gz}

mongorestore --drop --archive=${ARCHIVE} --gzip
