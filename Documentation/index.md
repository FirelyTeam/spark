# Spark FHIR server

## About
Spark is a public domain FHIR server developed in C#, initially built by Firely and as of recently being maintained by Incendi.

Spark implements a major part of the FHIR specification and has been used and tested during several HL7 WGM Connectathons.

Spark is the C# reference implementation of the FHIR specification. It supports all the resource types, all the search parameters and a lot of the more sophisticated interactions. To understand Spark and how you can use it, it is neccessary that you understand (at least the basics of) FHIR first.

Spark supports three versions of the FHIR specification: DSTU2, STU3 and R4.

The task of maintaining Spark is led by Incendi. Incendi will keep enhancing this server to support the latest versions and add functionality.

You can try a running instance of Spark on https://spark.incendi.no. Retrieve your first patient resource on https://spark.incendi.no/fhir/Patient/example. If you want to know about how Spark is structured, read about the [Architecture](Architecture.md). It makes it easier to understand everything else. Spark is open source and your [contribution](Contribute.md) is welcome. 

## Quickstart
You can follow the [quickstart guide](Quickstart.md) if you want to set up your own FHIR Server or Facade.

Code examples can be found [here](https://github.com/incendilabs/spark-example) for setting up a FHIR Server, and [here](https://github.com/incendilabs/spark-facade-example) for setting up a FHIR Facade.

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

