FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:254fe017495996d3abcc487ffacdcce2b73affb380d3af87af12c8dff41fe0b0 AS base

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80


FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:496914451b05ac5a24592024362ec370c3a2d3c3fe817f864e4aa55c4c194a56 AS build-deps

RUN apk add --no-cache nodejs npm


FROM build-deps AS npm-restore

WORKDIR /Applications/Spark.Web.Shared/app

COPY ["./Applications/Spark.Web.Shared/app/package.json", "./"]
COPY ["./Applications/Spark.Web.Shared/app/package-lock.json", "./"]

RUN npm ci


FROM build-deps AS dotnet-restore

WORKDIR /src

COPY ["./Directory.Build.props", "../Directory.Build.props"]
COPY ["./Libraries/Spark.Engine/Spark.Engine.csproj", "Libraries/Spark.Engine/Spark.Engine.csproj"]
COPY ["./Libraries/Spark.Store.MongoDB/Spark.Store.MongoDB.csproj", "Libraries/Spark.Store.MongoDB/Spark.Store.MongoDB.csproj"]
COPY ["./Libraries/Spark.Engine.R6/Spark.Engine.R6.csproj", "Libraries/Spark.Engine.R6/Spark.Engine.R6.csproj"]
COPY ["./Applications/Spark.Web.R6/Spark.Web.R6.csproj", "Applications/Spark.Web.R6/Spark.Web.R6.csproj"]

RUN dotnet restore "Applications/Spark.Web.R6/Spark.Web.R6.csproj"


FROM dotnet-restore AS build

WORKDIR /src

COPY --from=npm-restore /Applications/Spark.Web.Shared/app/node_modules ./Applications/Spark.Web.Shared/app/node_modules

COPY ["./Libraries/Spark.Engine/", "Libraries/Spark.Engine/"]
COPY ["./Libraries/Spark.Store.MongoDB/", "Libraries/Spark.Store.MongoDB/"]
COPY ["./Libraries/Spark.Engine.R6/", "Libraries/Spark.Engine.R6/"]
COPY ["./Libraries/Spark.Engine.Shared/", "Libraries/Spark.Engine.Shared/"]
COPY ["./Applications/Spark.Web.Shared/", "Applications/Spark.Web.Shared/"]
COPY ["./Applications/Spark.Web.R6/", "Applications/Spark.Web.R6/"]


FROM build AS publish


RUN dotnet publish "Applications/Spark.Web.R6/Spark.Web.R6.csproj" -c Release -o /app/publish --no-restore


FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Spark.Web.R6.dll"]
