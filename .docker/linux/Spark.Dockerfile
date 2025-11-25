FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80


# in this stage we install the build tools since we dont really need to reinstall them unless the base image changes.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build-deps

# install node.js and npm for frontend.
RUN apk add --no-cache nodejs npm

# Separate npm install from npm build
# package.json and package-lock.json change less frequently than source code. we do this to cache the layer across most code changes.
FROM build-deps AS npm-restore

WORKDIR /src/Spark.Web/ClientApp

# to make sure the layer "fails" if dependencies are updated.
COPY ["./src/Spark.Web/ClientApp/package.json", "./"]
COPY ["./src/Spark.Web/ClientApp/package-lock.json", "./"]

# we use 'npm ci' instead of 'npm install'
# Faster than npm install
# fra nett "Purpose: Designed for clean, consistent, and reproducible builds, 
#especially in automated environments like Continuous Integration/Continuous Deployment (CI/CD) pipelines."
RUN npm ci


# Separate NuGet restore from build
# .csproj files change less often than source code.
FROM build-deps AS dotnet-restore

WORKDIR /src

# Copy Directory.Build.props first. contains shared build settings
# required for restore to work (maybe haha)
COPY ["./Directory.Build.props", "../Directory.Build.props"]

# Copy .csproj files needed for restore
# correct order
COPY ["./src/Spark.Engine/Spark.Engine.csproj", "Spark.Engine/Spark.Engine.csproj"]
COPY ["./src/Spark.Mongo/Spark.Mongo.csproj", "Spark.Mongo/Spark.Mongo.csproj"]
COPY ["./src/Spark.Web/Spark.Web.csproj", "Spark.Web/Spark.Web.csproj"]

# The slow operation restore output is cached.
RUN dotnet restore "Spark.Web/Spark.Web.csproj"


#####
# under here we start building the actual application
#####

# This stage compiles the application. it will be invalidated on any code change. benefits from cached npm and NuGet packages from previous stages.
FROM dotnet-restore AS build

WORKDIR /src

# copy cached node_modules from npm-restore stage
# we don't need to run 'npm ci' again. just reuse the cached modules
COPY --from=npm-restore /src/Spark.Web/ClientApp/node_modules ./Spark.Web/ClientApp/node_modules

# copy frontend source files
COPY ["./src/Spark.Web/ClientApp/", "Spark.Web/ClientApp/"]

# this is the part that gets updated most often i think. so it's copied last
COPY ["./src/Spark.Engine/", "Spark.Engine/"]
COPY ["./src/Spark.Mongo/", "Spark.Mongo/"]
COPY ["./src/Spark.Web/", "Spark.Web/"]
COPY ["./src/Spark-Legacy/Examples/", "Spark-Legacy/Examples/"]


FROM build AS publish

RUN dotnet publish "Spark.Web/Spark.Web.csproj" -c Release -o /app/publish --no-restore


# final
# no SDK, no source code
FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Spark.Web.dll"]
