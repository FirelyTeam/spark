FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["./src/Spark.NetCore/", "Spark.NetCore/"]
COPY ["./src/Spark.Engine/", "Spark.Engine/"]
COPY ["./src/Spark.Mongo/", "Spark.Mongo/"]
RUN dotnet restore "/src/Spark.NetCore/Spark.NetCore.csproj"
COPY . .
RUN dotnet build "/src/Spark.NetCore/Spark.NetCore.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "/src/Spark.NetCore/Spark.NetCore.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
# COPY --from=build /src/Spark.NetCore/example_data/fhir_examples ./fhir_examples

ENTRYPOINT ["dotnet", "Spark.NetCore.dll"]