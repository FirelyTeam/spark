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

### ITransactionService and IAsyncTransactionService interface changes
IAsyncTransactionService has been removed and ITransactionService now has only async methods.

### TransactionService and AsyncTransactionService class changes
AsyncTransactionService has been removed and TransactionService now has only async methods.

### ITransactionHandler and IAsyncTransactionHandler interface changes
IAsyncTransactionHandler has been removed and ITransactionHandler now has only async methods.

### IServiceListener and ICompositeServiceListener interface changes
IServiceListener.Inform() has been removed, use IServiceListener.InformAsync() instead.
ICompositeServiceListener.Inform() has been removed, use ICompositeServiceListener.InformAsync() instead.

### IIndexService and IndexService changes
IIndexService.Process() and IIndexService.IndexResource() has been removed, use IIndexService.ProcessAsync() and
IIndexService.IndexResourceAsync() instead. The same is true for the implementation IndexService.

### IIndexStore and MongoIndexStore changes
IIndexStore.Save(), IIndexStore.Delete() and IIndexStore.Clean() has been removed, use IIndexStore.SaveAsync(),
IIndexStore.DeleteAsync() and IIndexStore.CleanAsync() instead. The same applies to the implementation MongoIndexStore. 

### IHistoryService and HistoryService changes
IHistoryService.History() has been removed, use IHistoryService.HistoryAsync() instead. The same applies to the
implementation HistoryService.

### IHistoryStore and HistoryStore changes
IHistoryStore.History() and overloads has been removed, use IHistoryService.HistoryAsync() instead. The same applies
to the implementation HistoryStore.

### ISearchService and SearchService changes
The following non-async methods has been removed:
- GetSnapshot()
- GetSnapshotForEverything()
- FindSingle()
- FindSingleOrDefault()
- GetSearchResults()
