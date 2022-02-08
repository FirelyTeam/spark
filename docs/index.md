---
name: Introduction
route: /
---

# Spark FHIR server

## About
Spark is a public domain FHIR server developed in C#, initially built by Firely and as of recently being maintained by Kufu.

Spark implements a major part of the FHIR specification and has been used and tested during several HL7 WGM Connectathons.

Spark is the C# reference implementation of the FHIR specification. It supports all the resource types, all the search parameters and a lot of the more sophisticated interactions. To understand Spark and how you can use it, it is neccessary that you understand (at least the basics of) FHIR first.

Spark supports three versions of the FHIR specification: DSTU2, STU3 and R4. 

As of recently the task of maintaining Spark has been taken upon by the community and is led by Kufu.
Kufu and the community, will keep enhancing this server to support the latest versions and add functionality.
We also welcome anyone who wants to support this effort and help us make Spark a better reference
platform and playground for FHIR.

You can try a running instance of Spark on https://spark.kufu.no. Retrieve your first patient resource on https://spark.kufu.no/fhir/Patient/example. If you want to know about how Spark is structured, read about the [Architecture](./architecture). It makes it easier to understand everything else. Spark is open source and your [contribution](./Contribute) is welcome. 

## Quickstart
The easiest way to test Spark FHIR server is by using Docker. Make sure you have installed [Docker](https://docs.docker.com/install/). On Linux you will need to install [Docker Compose](https://docs.docker.com/compose/install/) as well. After installing Docker you could run Spark server by running the command for your operating system: 

R4:
 * Mac OS X / Windows: `curl 'https://raw.githubusercontent.com/firelyteam/spark/develop/dockers/spark-r4/docker-compose.yml' > docker-compose.yml && docker-compose up`
 * Linux: `curl 'https://raw.githubusercontent.com/FirelyTeam/spark/develop/dockers/spark-r4/docker-compose.yml' > docker-compose.yml && sudo docker-compose up`

STU3:
 * Mac OS X / Windows: `curl 'https://raw.githubusercontent.com/firelyteam/spark/develop/dockers/spark-stu3/docker-compose.yml' > docker-compose.yml && docker-compose up`
 * Linux: `curl 'https://raw.githubusercontent.com/FirelyTeam/spark/develop/dockers/spark-stu3/docker-compose.yml' > docker-compose.yml && sudo docker-compose up`

DSTU2:
 * Mac OS X / Windows: `curl 'https://raw.githubusercontent.com/firelyteam/spark/develop/dockers/spark-dstu2/docker-compose.yml' > docker-compose.yml && docker-compose up`
 * Linux: `curl 'https://raw.githubusercontent.com/FirelyTeam/spark/develop/dockers/spark-dstu2/docker-compose.yml' > docker-compose.yml && sudo docker-compose up`

Spark FHIR server will be available after startup at `http://localhost:5555`.

