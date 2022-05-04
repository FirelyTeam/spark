FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build
WORKDIR /src
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
