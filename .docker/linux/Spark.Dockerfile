FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["./src/Spark.Web/", "Spark.Web/"]
COPY ["./src/Spark.Engine/", "Spark.Engine/"]
COPY ["./src/Spark.Mongo/", "Spark.Mongo/"]
COPY ["./src/Spark/Examples", "Spark/Examples"]
RUN dotnet restore "/src/Spark.Web/Spark.Web.csproj"
COPY . .
RUN dotnet build "/src/Spark.Web/Spark.Web.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "/src/Spark.Web/Spark.Web.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
# COPY --from=build /src/Spark.Web/example_data/fhir_examples ./fhir_examples

ENTRYPOINT ["dotnet", "Spark.Web.dll"]
