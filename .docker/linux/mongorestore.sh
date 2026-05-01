#!/bin/bash

set -e

mongorestore --drop --archive=${ARCHIVE} --gzip
