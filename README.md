Spark FHIR
=====

Spark is an open-source FHIR server developed in C#, initially built by Firely. Further development 
and maintenance is now done by Incendi.

Spark implements a major part of the FHIR specification and has been used and tested during several
HL7 WGM Connectathons.

### Getting started
There are three ways to get started with Spark. Either by using the NuGet packages and following [the Quickstart Tutorial](Documentation/Quickstart.md), by using the Docker Images, or building from source.

#### NuGet packages
Read [the Quickstart Tutorial](Documentation/Quickstart.md) on how to set up your own FHIR Server using the NuGet Packages. There is also an example project that accompanies [the Quickstart Tutorial](Documentation/Quickstart.md) which you can find here: https://github.com/incendilabs/spark-example

#### Docker images
Set up the Spark FHIR server by using the Docker Images. Make sure you have installed [Docker](https://docs.docker.com/install/). On Linux you will need to install [Docker Compose](https://docs.docker.com/compose/install/) as well. After installing Docker you could run Spark server by running one of the following commands, found below, for your preferred FHIR Version. Remember to replace the single quotes with double quotes on Windows. The Spark FHIR Server will be available after startup at `http://localhost:5555`.

#### R4
```
curl 'https://raw.githubusercontent.com/FirelyTeam/spark/r4/master/.docker/docker-compose.example.yml' > docker-compose.yml
docker-compose up
```
#### STU3
```
curl 'https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/.docker/docker-compose.example.yml' > docker-compose.yml
docker-compose up`
```

## Versions

#### R4
Source code can be found in the branch **master**. This is the version of Spark running at https://spark.incendi.no
FHIR Endpoint: https://spark.incendi.no/fhir

#### STU3
Source code can be found in the branch **master**, we try to keep up-to-date with the STU3 version of FHIR.
This is the version of Spark running at https://spark-stu3.incendi.no FHIR Endpoint: https://spark-stu3.incendi.no/fhir

#### DSTU2
DSTU2 is no longer maintained by this project. The source code can be found in the branch **dstu2/master**.

#### DSTU1
DSTU1 is no longer maintained by this project. The source code can be found in the branch **dstu1/master**.

## Get in touch and participate

Join [our Discord server](https://discord.gg/M9kp2zGGYF) to participate in development discussion.

If you want to contribute, see [our guidelines](CONTRIBUTING.md)
