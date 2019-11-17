---
name: Spark FHIR server
route: /
---

# Spark FHIR server
Spark is a public domain FHIR server developed in C#, initially built by Firely and as of recently being
maintained by Kufu.

Spark implements a major part of the FHIR specification and has been used and tested during several
HL7 WGM Connectathons.

As of recently the task of maintaining Spark has been taken upon by the community and is led by Kufu.
Kufu and the community, will keep enhancing this server to support the latest versions and add functionality.
We also welcome anyone who wants to support this effort and help us make Spark a better reference
platform and playground for FHIR.


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
