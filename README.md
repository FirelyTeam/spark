|DSTU2|STU3|R4
|---|---|---
|![Tests](https://github.com/FirelyTeam/spark/workflows/Tests/badge.svg?branch=develop)|![Tests](https://github.com/FirelyTeam/spark/workflows/Tests/badge.svg?branch=stu3%2Fdevelop)|![Tests](https://github.com/FirelyTeam/spark/workflows/Tests/badge.svg?branch=r4%2Fdevelop)

Spark
=====

Spark is a public domain FHIR server developed in C#, initially built by Firely and as of recently being
maintained by Kufu.

Spark implements a major part of the FHIR specification and has been used and tested during several
HL7 WGM Connectathons.

As of recently the task of maintaining Spark has been taken upon by the community and is led by Kufu.
Kufu and the community, will keep enhancing this server to support the latest versions and add functionality.
We also welcome anyone who wants to support this effort and help us make Spark a better reference
platform and playground for FHIR.

**DISCLAIMER: The web projects Spark.Web and Spark are meant as reference implementations and should never be used out of the box in a production environment without adding as a minimum security features.**

## Quickstart
The easiest way to test Spark FHIR server is by using Docker. Make sure you have installed [Docker](https://docs.docker.com/install/). On Linux you will need to install [Docker Compose](https://docs.docker.com/compose/install/) as well. After installing Docker you could run Spark server by running the command for your operating system: 

DSTU2:
 * Mac OS X / Windows: `curl 'https://raw.githubusercontent.com/firelyteam/spark/develop/dockers/spark-dstu2/docker-compose.yml' > docker-compose.yml && docker-compose up`
 * Linux: `curl 'https://raw.githubusercontent.com/FirelyTeam/spark/develop/dockers/spark-dstu2/docker-compose.yml' > docker-compose.yml && sudo docker-compose up`

STU3:
 * Mac OS X / Windows: `curl 'https://raw.githubusercontent.com/firelyteam/spark/develop/dockers/spark-stu3/docker-compose.yml' > docker-compose.yml && docker-compose up`
 * Linux: `curl 'https://raw.githubusercontent.com/FirelyTeam/spark/develop/dockers/spark-stu3/docker-compose.yml' > docker-compose.yml && sudo docker-compose up`

R4:
 * Mac OS X / Windows: `curl 'https://raw.githubusercontent.com/firelyteam/spark/develop/dockers/spark-r4/docker-compose.yml' > docker-compose.yml && docker-compose up`
 * Linux: `curl 'https://raw.githubusercontent.com/FirelyTeam/spark/develop/dockers/spark-r4/docker-compose.yml' > docker-compose.yml && sudo docker-compose up`

Spark FHIR server will be available after startup at `http://localhost:5555`.

## Versions

#### DSTU1
DSTU1 is no longer maintained by this project. The source code can be found in the branch **dstu1/master**.

#### DSTU2
Source code can be found in the branch **master**, we try to keep up-to-date with the DSTU2 version of FHIR.

#### STU3
Source code can be found in the branch **stu3/master**, we try to keep up-to-date with the STU3 version of FHIR.

#### R4
Source code can be found in the branch **r4/master**. This is the version of Spark running at http://spark.kufu.no
FHIR Endpoint: http://spark.kufu.no/fhir

#### Contributing
If you want to contribute, see our [guidelines](https://github.com/furore-fhir/spark/wiki/Contributing)
