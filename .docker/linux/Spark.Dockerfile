FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:21c3f9798416cba4c0576a947c286231cb71ea2884896cb968253f5c9668f930 AS base

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80


FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:0294f285d062afacc8b5f6d7febb0cd4ee879320e5585fa15f2f4ebf1feb9ba7 AS build-deps

RUN apk add --no-cache nodejs npm


FROM build-deps AS npm-restore

WORKDIR /src/Spark.Web/app

COPY ["./src/Spark.Web/app/package.json", "./"]
COPY ["./src/Spark.Web/app/package-lock.json", "./"]

RUN npm ci


FROM build-deps AS dotnet-restore

WORKDIR /src

COPY ["./Directory.Build.props", "../Directory.Build.props"]
COPY ["./src/Spark.Engine/Spark.Engine.csproj", "Spark.Engine/Spark.Engine.csproj"]
COPY ["./src/Spark.Mongo/Spark.Mongo.csproj", "Spark.Mongo/Spark.Mongo.csproj"]
COPY ["./src/Spark.Web/Spark.Web.csproj", "Spark.Web/Spark.Web.csproj"]

RUN dotnet restore "Spark.Web/Spark.Web.csproj"


FROM dotnet-restore AS build

WORKDIR /src

COPY --from=npm-restore /src/Spark.Web/app/node_modules ./Spark.Web/app/node_modules

COPY ["./src/Spark.Web/app/", "Spark.Web/app/"]

COPY ["./src/Spark.Engine/", "Spark.Engine/"]
COPY ["./src/Spark.Mongo/", "Spark.Mongo/"]
COPY ["./src/Spark.Web/", "Spark.Web/"]
COPY ["./src/Spark-Legacy/Examples/", "Spark-Legacy/Examples/"]


FROM build AS publish


RUN dotnet publish "Spark.Web/Spark.Web.csproj" -c Release -o /app/publish --no-restore


FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Spark.Web.dll"]
