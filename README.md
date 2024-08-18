|DSTU2|STU3|R4
|:-:|:-:|:-:
|![Tests](https://github.com/FirelyTeam/spark/actions/workflows/run_tests.yaml/badge.svg?branch=master)|![Tests](https://github.com/FirelyTeam/spark/actions/workflows/run_tests.yaml/badge.svg?branch=stu3%2Fmaster)|![Tests](https://github.com/FirelyTeam/spark/actions/workflows/run_tests.yaml/badge.svg?branch=r4%2Fmaster)
|![Integration Tests](https://github.com/FirelyTeam/spark/actions/workflows/integration_tests.yml/badge.svg?branch=master)|![Integration Tests](https://github.com/FirelyTeam/spark/actions/workflows/integration_tests.yml/badge.svg?branch=stu3%2Fmaster)|![Integration Tests](https://github.com/FirelyTeam/spark/actions/workflows/integration_tests.yml/badge.svg?branch=r4%2Fmaster)
|![Release](https://github.com/FirelyTeam/spark/actions/workflows/nuget_deploy.yml/badge.svg)|![Release](https://github.com/FirelyTeam/spark/actions/workflows/nuget_deploy.yml/badge.svg)|![Release](https://github.com/FirelyTeam/spark/actions/workflows/nuget_deploy.yml/badge.svg)
|![Docker Release](https://github.com/FirelyTeam/spark/actions/workflows/docker_image_linux.yml/badge.svg)|![Docker Release](https://github.com/FirelyTeam/spark/actions/workflows/docker_image_linux.yml/badge.svg)|![Docker Release](https://github.com/FirelyTeam/spark/actions/workflows/docker_image_linux.yml/badge.svg)

Spark
=====

Spark is an open-source FHIR server developed in C#, initially built by Firely. Further development 
and maintenance is now done by Incendi.

Spark implements a major part of the FHIR specification and has been used and tested during several
HL7 WGM Connectathons.

### Get Started
There are two ways to get started with Spark. Either by using the NuGet packages and following the Quickstart Tutorial, or by using the Docker Images.

#### NuGet Packages
Read the [Quickstart Tutorial](Documentation/Quickstart.md) on how to set up your own FHIR Server using the NuGet Packages. There is also an example project that accompanies the Quickstart Tutorial which you can find here: https://github.com/incendilabs/spark-example

#### Docker Images
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

#### DSTU2
```
curl 'https://raw.githubusercontent.com/FirelyTeam/spark/master/.docker/docker-compose.example.yml' > docker-compose.yml 
docker-compose up
```

## Versions

#### R4
Source code can be found in the branch **r4/master**. This is the version of Spark running at https://spark.incendi.no
FHIR Endpoint: https://spark.incendi.no/fhir

#### STU3
Source code can be found in the branch **stu3/master**, we try to keep up-to-date with the STU3 version of FHIR.
This is the version of Spark running at https://spark-stu3.incendi.no FHIR Endpoint: https://spark-stu3.incendi.no/fhir

#### DSTU2
DSTU2 is no longer maintained by this project. The source code can be found in the branch **master**.

#### DSTU1
DSTU1 is no longer maintained by this project. The source code can be found in the branch **dstu1/master**.

## Contributing
If you want to contribute, see our [guidelines](https://github.com/FirelyTeam/spark/wiki/Contributing)

### Git branching strategy
Our strategy for git branching:

Branch from the `r4/master` branch which contains the R4 FHIR version, unless the feature or bug fix is considered for a specific version of FHIR then branch from the relevant branch which at this point is `stu3/master`.

See [GitHub flow](https://guides.github.com/introduction/flow/) for more information.
