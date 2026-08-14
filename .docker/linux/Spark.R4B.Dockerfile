FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:1b3a34768687d583ebdf16ca8dd9a21ec93de9cdf81bb424e3c1a706e2a453d7 AS base

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80


FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:620e765fe18186c08399f7aa978f79f04b6bbf0ee1b3b8a91e2d5c9619e59da1 AS build-deps

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
COPY ["./Libraries/Spark.Engine.R4B/Spark.Engine.R4B.csproj", "Libraries/Spark.Engine.R4B/Spark.Engine.R4B.csproj"]
COPY ["./Applications/Spark.Web.R4B/Spark.Web.R4B.csproj", "Applications/Spark.Web.R4B/Spark.Web.R4B.csproj"]

RUN dotnet restore "Applications/Spark.Web.R4B/Spark.Web.R4B.csproj"


FROM dotnet-restore AS build

WORKDIR /src

COPY --from=npm-restore /Applications/Spark.Web.Shared/app/node_modules ./Applications/Spark.Web.Shared/app/node_modules

COPY ["./Libraries/Spark.Engine/", "Libraries/Spark.Engine/"]
COPY ["./Libraries/Spark.Store.MongoDB/", "Libraries/Spark.Store.MongoDB/"]
COPY ["./Libraries/Spark.Engine.R4B/", "Libraries/Spark.Engine.R4B/"]
COPY ["./Libraries/Spark.Engine.Shared/", "Libraries/Spark.Engine.Shared/"]
COPY ["./Applications/Spark.Web.Shared/", "Applications/Spark.Web.Shared/"]
COPY ["./Applications/Spark.Web.R4B/", "Applications/Spark.Web.R4B/"]


FROM build AS publish


RUN dotnet publish "Applications/Spark.Web.R4B/Spark.Web.R4B.csproj" -c Release -o /app/publish --no-restore


FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Spark.Web.R4B.dll"]
