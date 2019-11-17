---
name: Installing Spark
route: /install
menu: Get started
---

# Getting Started

## Database

Spark FHIR server supports MongoDB as it´s persistence layer. The following options are supported:

- MongoDB Atlas. Sign up for a [free trial account](https://www.mongodb.com/download-center) and run your database in the cloud.
- MongoDB Community Server. [Download](https://www.mongodb.com/download-center/community) and install locally
- MongoDB Enterprise. [Download](https://www.mongodb.com/download-center/enterprise) and install locally.
- MongoDB in docker. Checkt the example [docker-compose.yml](https://github.com/FirelyTeam/spark/blob/master/docker-compose.yml) for setup.

## Install Spark

The core packages Spark.Eninge and Spark.Mongo are running on NetStandard 2.0. For the web applications you choose between:

- `Spark.Web` which runs on ASP.Net Core 2.1, or
- `Spark.Classic` which runs on ASP.Net 4.6 or later.
