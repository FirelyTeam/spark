# Getting Started

## Database

Spark FHIR server supports MongoDB as it´s persistence layer. The following options are supported:

- MongoDB Atlas. Sign up for a [free trial account](https://www.mongodb.com/download-center) and run your database in the cloud.
- MongoDB Community Server. [Download](https://www.mongodb.com/download-center/community) and install locally
- MongoDB Enterprise. [Download](https://www.mongodb.com/download-center/enterprise) and install locally.
- MongoDB in docker. Check out the example [docker-compose-example.yml](../.docker/docker-compose.example.yml).

## CosmoDB with MongoDB as a database
In general we do not recommend using CosmosDB. There are known installations using CosmosDB with MongoDB API which runs fairly well, but it has not been without problems.

## Install Spark

The core packages `Spark.Engine` and `Spark.Mongo` targets `net8.0`, `net9.0`, and `net10.0`.

## Reference Implementation
The reference implementation are meant as an example and must not be used out of the box in a production environment without adding security features.

The reference implementation is:
- `Spark.Web` which is an ASP.NET 10.0 web application.
