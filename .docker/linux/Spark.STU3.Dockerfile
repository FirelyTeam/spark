FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80


FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build-deps

RUN apk add --no-cache nodejs npm


FROM build-deps AS npm-restore

WORKDIR /Applications/Spark.Web.STU3/app

COPY ["./Applications/Spark.Web.STU3/app/package.json", "./"]
COPY ["./Applications/Spark.Web.STU3/app/package-lock.json", "./"]

RUN npm ci


FROM build-deps AS dotnet-restore

WORKDIR /src

COPY ["./Directory.Build.props", "../Directory.Build.props"]
COPY ["./Libraries/Spark.Engine/Spark.Engine.csproj", "Libraries/Spark.Engine/Spark.Engine.csproj"]
COPY ["./Libraries/Spark.Store.MongoDB/Spark.Store.MongoDB.csproj", "Libraries/Spark.Store.MongoDB/Spark.Store.MongoDB.csproj"]
COPY ["./Libraries/Spark.Engine.STU3/Spark.Engine.STU3.csproj", "Libraries/Spark.Engine.STU3/Spark.Engine.STU3.csproj"]
COPY ["./Applications/Spark.Web.STU3/Spark.Web.STU3.csproj", "Applications/Spark.Web.STU3/Spark.Web.STU3.csproj"]

RUN dotnet restore "Applications/Spark.Web.STU3/Spark.Web.STU3.csproj"


FROM dotnet-restore AS build

WORKDIR /src

COPY --from=npm-restore /Applications/Spark.Web.STU3/app/node_modules ./Applications/Spark.Web.STU3/app/node_modules

COPY ["./Applications/Spark.Web.STU3/app/", "Applications/Spark.Web.STU3/app/"]

COPY ["./Libraries/Spark.Engine/", "Libraries/Spark.Engine/"]
COPY ["./Libraries/Spark.Store.MongoDB/", "Libraries/Spark.Store.MongoDB/"]
COPY ["./Libraries/Spark.Engine.STU3/", "Libraries/Spark.Engine.STU3/"]
COPY ["./Libraries/Spark.Engine.Shared/", "Libraries/Spark.Engine.Shared/"]
COPY ["./Applications/Spark.Web.Shared/", "Applications/Spark.Web.Shared/"]
COPY ["./Applications/Spark.Web.STU3/", "Applications/Spark.Web.STU3/"]


FROM build AS publish


RUN dotnet publish "Applications/Spark.Web.STU3/Spark.Web.STU3.csproj" -c Release -o /app/publish --no-restore


FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Spark.Web.STU3.dll"]
