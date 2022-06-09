# Getting Started

## Database

Spark FHIR server supports MongoDB as it´s persistence layer. The following options are supported:

- MongoDB Atlas. Sign up for a [free trial account](https://www.mongodb.com/download-center) and run your database in the cloud.
- MongoDB Community Server. [Download](https://www.mongodb.com/download-center/community) and install locally
- MongoDB Enterprise. [Download](https://www.mongodb.com/download-center/enterprise) and install locally.
- MongoDB in docker. Checkt the example [docker-compose-example.yml](../.docker/docker-compose.example.yml) for setup.
- CosmosDB with MongoDB API

## Install Spark

The core packages `Spark.Engine` and `Spark.Mongo` are targetting on `netstandard 2.0`. For the web application you may choose between:

## Reference Implementations
The reference implementations are only meant as examples and must never be used out of the box in a production environment without adding as a minimum security features.

- `Spark.Web` which runs on ASP.Net Core 3.1.
- `Spark` which runs on ASP.Net 4.7.2.
