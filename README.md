|DSTU2|STU3|R4
|:-:|:-:|:-:
|![Tests](https://github.com/FirelyTeam/spark/workflows/Tests/badge.svg?branch=develop)|![Tests](https://github.com/FirelyTeam/spark/workflows/Tests/badge.svg?branch=stu3%2Fdevelop)|![Tests](https://github.com/FirelyTeam/spark/workflows/Tests/badge.svg?branch=r4%2Fdevelop)
|![Release](https://github.com/FirelyTeam/spark/workflows/Release/badge.svg)|![Release](https://github.com/FirelyTeam/spark/workflows/Release/badge.svg)|![Release](https://github.com/FirelyTeam/spark/workflows/Release/badge.svg)
|![Docker Release](https://github.com/FirelyTeam/spark/workflows/Docker%20Release/badge.svg)|![Docker Release](https://github.com/FirelyTeam/spark/workflows/Docker%20Release/badge.svg)|![Docker Release](https://github.com/FirelyTeam/spark/workflows/Docker%20Release/badge.svg)

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
The easiest way to test Spark FHIR server is by using Docker. Make sure you have installed [Docker](https://docs.docker.com/install/). On Linux you will need to install [Docker Compose](https://docs.docker.com/compose/install/) as well. After installing Docker you could run Spark server by running one of the following commands, found below, for your preferred FHIR Version. Remember to replace the single quotes with double quotes on Windows. The Spark FHIR Server will be available after startup at `http://localhost:5555`.

#### DSTU2
`curl 'https://raw.githubusercontent.com/FirelyTeam/spark/master/.docker/docker-compose.example.yml' > docker-compose.yml && docker-compose up`

#### STU3
`curl 'https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/.docker/docker-compose.example.yml' > docker-compose.yml && docker-compose up`

#### R4
`curl 'https://raw.githubusercontent.com/FirelyTeam/spark/r4/master/.docker/docker-compose.example.yml' > docker-compose.yml && docker-compose up`

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

## Contributing
If you want to contribute, see our [guidelines](https://github.com/furore-fhir/spark/wiki/Contributing)

### Git branching strategy
Our strategy for git branching:

Branch from the master branch which contains the DSTU2 version, unless the feature or bug fix is considered for a specific version of FHIR then branch from either stu3/master or r4/master.

See [GitHub flow](https://guides.github.com/introduction/flow/) for more information.
