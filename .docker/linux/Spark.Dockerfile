FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:1b3a34768687d583ebdf16ca8dd9a21ec93de9cdf81bb424e3c1a706e2a453d7 AS base

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80


FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:5c559aa5d99337e400d39ab4fa1f6979d126c29b20939d53658ed38300571e74 AS build-deps

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
