FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
WORKDIR /src
COPY ["./Directory.Build.props", "../Directory.Build.props"]
COPY ["./src/Spark.Web/Spark.Web.csproj", "Spark.Web/Spark.Web.csproj"]
COPY ["./src/Spark.Engine/Spark.Engine.csproj", "Spark.Engine/Spark.Engine.csproj"]
COPY ["./src/Spark.Mongo/Spark.Mongo.csproj", "Spark.Mongo/Spark.Mongo.csproj"]
RUN dotnet restore "/src/Spark.Web/Spark.Web.csproj"
COPY ./src .
RUN dotnet build "/src/Spark.Web/Spark.Web.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "/src/Spark.Web/Spark.Web.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ENTRYPOINT ["dotnet", "Spark.Web.dll"]
