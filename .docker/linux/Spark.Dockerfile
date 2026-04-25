FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80


FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build-deps

RUN apk add --no-cache nodejs npm


FROM build-deps AS npm-restore

WORKDIR /Applications/Spark.Web/app

COPY ["./Applications/Spark.Web/app/package.json", "./"]
COPY ["./Applications/Spark.Web/app/package-lock.json", "./"]

RUN npm ci


FROM build-deps AS dotnet-restore

WORKDIR /src

COPY ["./Directory.Build.props", "../Directory.Build.props"]
COPY ["./Libraries/Spark.Engine/Spark.Engine.csproj", "Libraries/Spark.Engine/Spark.Engine.csproj"]
COPY ["./Libraries/Spark.Mongo/Spark.Mongo.csproj", "Libraries/Spark.Mongo/Spark.Mongo.csproj"]
COPY ["./Libraries/Spark.Engine.R4/Spark.Engine.R4.csproj", "Libraries/Spark.Engine.R4/Spark.Engine.R4.csproj"]
COPY ["./Applications/Spark.Web/Spark.Web.csproj", "Applications/Spark.Web/Spark.Web.csproj"]

RUN dotnet restore "Applications/Spark.Web/Spark.Web.csproj"


FROM dotnet-restore AS build

WORKDIR /src

COPY --from=npm-restore /Applications/Spark.Web/app/node_modules ./Applications/Spark.Web/app/node_modules

COPY ["./Applications/Spark.Web/app/", "Applications/Spark.Web/app/"]

COPY ["./Libraries/Spark.Engine/", "Libraries/Spark.Engine/"]
COPY ["./Libraries/Spark.Mongo/", "Libraries/Spark.Mongo/"]
COPY ["./Libraries/Spark.Engine.R4/", "Libraries/Spark.Engine.R4/"]
COPY ["./Applications/Spark.Web/", "Applications/Spark.Web/"]


FROM build AS publish


RUN dotnet publish "Applications/Spark.Web/Spark.Web.csproj" -c Release -o /app/publish --no-restore


FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Spark.Web.dll"]
