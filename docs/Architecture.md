---
name: Spark Architecture
route: /architecture
menu: Get started
---

# Spark FHIR server

Spark is built in three layers:

1. Spark Server (Spark.Web for Asp.net core 2.1, or Spark.csproj for ASP.net 4.6): An ASP.Net MVC application hosting both a (minimal) visual interface, the FHIR (REST) endpoint and a Maintenance operation.
2. Spark Engine (Spark.Engine.csproj): The implementation of everything FHIR: the REST interface, indexing of the search parameters and interpreting search requests, construction of FHIR responses etc.
3. Spark Mongo (Spark.Mongo.csproj): Storage and retrieval of both resources and the index based on MongoDB.

# Spark is built on the .NET FHIR API

Spark uses the .NET FHIR API to parse and serialize resources, and as a source of metadata about the FHIR specification: what Resource types are available, what is the definition of the SearchParameters and so on. The parsing and serialization in this API is heavily optimized. Using Spark you get the benefits of that. It also means that Spark is bound to a specific version of the API, which is currently the DSTU2 version.

# Spark Engine

Spark Engine provides:

1. Interfaces for the various functions that must be implemented by the storage layer: 
    * IFhirStore: Add and retrieve resources.
    * IFhirIndex: Process resources to index entries, search resources using the index.
    * IIndexStore: Save and delete index entries.
    * ISnapShotStore: Save and retrieve snapshots of search results for paging.
    * IHistoryStore: Get previous versions of a resource, a resource type or the whole system.
    * IGenerator: Generate ID's for new resources and new versions of resources.
2. Services for handling generic FHIR functionality
    * SearchService: Combine IFhirIndex and ISnapShotStore to paging results.
    * ElementIndexer: Translate FHIR DataTypes to parts of an Index entry.
    * ConformanceService: Provide the conformance statement of Spark.
3. ASP.NET Filters for handling cross cutting FHIR concerns:
    * Translating Exceptions to FHIR Response Messages (an http error or OperationOutcome)
    * (De)compression of request and response data.
4. ASP.NET Formatters for several wire formats:
    * JSON
    * XML
    * Binary
    * HTML
5. All kinds of model and helper classes to assist in the functions above.

This all accumulates in (I)FhirService, that presents all the FHIR functions.

# Spark Mongo

The MongoDB implementation of Spark stores the resources, the index, the snapshots and the generated ID's in MongoDB, with one collection for each. Previous versions of resources are also in the Resources collection. The Index collection contains only the current version of every resource.

MongoSearcher implements the actual searching mechanism on the MongoDB index, using the generic ResourceVisitor and Criterium classes in Spark Engine.

Be aware that MongoDB is heavily used (especially on searches), so it should be on an endpoint with very little latency.