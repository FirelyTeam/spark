## Running Spark in Docker

Spark can run in a Docker. This is a sample Dockerfile for running Spark.

```
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-stretch AS build
WORKDIR /src
COPY ["./Applications/Spark.Web/", "Applications/Spark.Web/"]
COPY ["./Libraries/Spark.Engine/", "Libraries/Spark.Engine/"]
COPY ["./Libraries/Spark.Engine.R4/", "Libraries/Spark.Engine.R4/"]
COPY ["./Libraries/Spark.Mongo/", "Libraries/Spark.Mongo/"]
RUN dotnet restore "/Applications/Spark.Web/Spark.Web.csproj"
COPY . .
RUN dotnet build "/Applications/Spark.Web/Spark.Web.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "/Applications/Spark.Web/Spark.Web.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
# COPY --from=build /Applications/Spark.Web/example_data/fhir_examples ./fhir_examples

ENTRYPOINT ["dotnet", "Spark.Web.dll"]
```

If you want to run the Mongo database as well, you could configure it with a `docker-compse.yml`-file:

```
version: "3"
services:
  spark:
    container_name: spark
    restart: always
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      - StoreSettings__ConnectionString=mongodb://root:CosmicTopSecret@mongodb:27017/spark?authSource=admin
      - SparkSettings__Endpoint=http://localhost:5555/fhir
    ports:
      - "5555:80"
      - "44348:443"
    links:
      - mongodb
    depends_on:
      - mongodb
  mongodb:
    container_name: mongodb
    image: mongo
    environment:
      MONGO_INITDB_ROOT_USERNAME: root
      MONGO_INITDB_ROOT_PASSWORD: CosmicTopSecret
    ports:
      - "17017:27017"
  mongosetup:
    container_name: mongosetup
    image: mongo
    volumes:
      - ./Applications/Spark.Web/example_data/db_dump:/data/db_dump
    depends_on:
      - mongodb
    links:
      - mongodb
    entrypoint:
      ["mongorestore", "--uri=mongodb://root:CosmicTopSecret@mongodb:27017/spark", "--drop", "--archive=/data/db_dump/dstu2.archive.gz", "--gzip"]
    environment:
      WAIT_HOSTS: mongodb:27017
```
