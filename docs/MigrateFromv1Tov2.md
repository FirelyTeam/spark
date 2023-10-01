## Migrate from 1.0 to 2.0

### Interfaces and classes that have been renamed
- IGenerator has been renamed to IIdentityGenerator

### New IGenerator implementation
MongoIdGenerator is replaced by GuidGenerator and is the new default identity generator for resource's.
See issue https://github.com/FirelyTeam/spark/issues/572 for more information.

### Rebuild indexes
Better resolve handling for search parameters of type reference has been added, for this to take effect on existing
resources the index has to be rebuilt. This can be done by by using the Admin UI that comes with Spark.Web or wire up
IndexRebuildService in your implementation.

### IFhirService and IAsyncFhirService interface changes
IAsyncFhirService has been removed and IFhirService now has only async methods.

### FhirServiceBase and AsyncFhirServiceBase class changes
AsyncFhirServiceBase has been removed and FhirServiceBase now has only async methods.

### FhirService and AsyncFhirService class changes
AsyncFhirService has been removed and FhirService now has only async methods.
