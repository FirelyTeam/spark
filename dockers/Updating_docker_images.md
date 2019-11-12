# Dockers and docker composers

## Update Spark images on Docker hub

In project root folder for each of the branches `develop`, `stu3/develop` and `r4/develop`: 

```bash
docker build -t spark-version .
docker tag spark-fhirversion sparkfhir/spark-fhirversion
docker login
docker push sparkfhir/spark-fhirversion
```

## Updating Mongo images with preloaded examples on Docker Hub

In each `dockers/mongo-spark-fhirversion directory`:

```bash
docker build -t mongo-spark-fhirversion .
docker tag mongo-spark-fhirversion sparkfhir/mongo-spark-fhirversion:pre-release
docker login
docker push sparkfhir/mongo-spark-fhirversion:pre-release
```

