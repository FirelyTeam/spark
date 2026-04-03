## Migrate from v2 to v3

### Target Frameworks
We now target `net8.0`, `net9.0`, and `net10.0`. `netstandard2.0` and `net472` targets have been removed.

### New classes and interfaces
- `FhirResponse<T>` wraps a `FhirResponse`, with the generic parameter representing the FHIR resource type.
- `IIndexQueue` (`Spark.Engine.Store.Interfaces`) — interface for durable index queue operations: `EnqueueAsync`, `ClaimNextAsync`, `AcknowledgeAsync`, `NackAsync`.
- `IndexQueueEntry` (`Spark.Engine.Core`) — model returned by `IIndexQueue.ClaimNextAsync`; carries the `Entry`, attempt count, and last error.
- `IndexQueueSettings` (`Spark.Engine.Store`) — configuration for index queue behavior: `LeaseTimeout`, `MaxAttempts`, `PollInterval`. Exposed as `StoreSettings.IndexQueue`.
- `MongoIndexQueue` (`Spark.Mongo.Store`) — MongoDB implementation of `IIndexQueue` backed by the `indexqueue` collection.
- `IndexWorker` (`Spark.Engine.Service`) — `BackgroundService` that polls `IIndexQueue` and drains pending entries via `IIndexService`. Registered automatically when `SparkSettings.Experimental.IndexingMode = Background`.
- `IndexServiceListener` (`Spark.Engine.Service`) — `IServiceListener` that processes search index updates synchronously in the HTTP request path. Registered by default (`IndexingMode = Synchronous`).
- `IndexQueueEnqueueListener` (`Spark.Engine.Service`) — `IServiceListener` that enqueues write events onto `IIndexQueue` for asynchronous background processing. Registered when `IndexingMode = Background`.
- `ExperimentalSettings` (`Spark.Engine`) — groups experimental settings under `SparkSettings.Experimental`. Currently exposes `IndexingMode`.
- `IndexingMode` (`Spark.Engine`) — enum that controls the indexing strategy: `Synchronous` (default) or `Background`. Accessed via `SparkSettings.Experimental.IndexingMode`.

### IFhirService, FhirServiceBase and FhirService changes
- New generic methods:
  - `Task<FhirResponse<T>> ReadAsync<T>(IKey, ConditionalHeaderParameters) where T : Resource`
  - `Task<FhirResponse<T>> VersionReadAsync<T>(IKey, ConditionalHeaderParameters) where T : Resource`

### IFhirResponseFactory and FhirResponseFactory changes
- New generic methods:
  - `FhirResponse<T> GetFhirResponse<T>(Entry entry, IKey key = null, IEnumerable<object> parameters = null) where T : Resource`
  - `FhirResponse<T> GetFhirResponse<T>(Entry entry, IKey key = null, params object[] parameters) where T : Resource`

### Snapshot changes
- `Snapshot.NOCOUNT` constant has been removed.
- `Snapshot.MAX_PAGE_SIZE` is now `private`.
- `Snapshot.CreateKey()` is now `private`.
- All settable properties (`Type`, `Keys`, `FeedSelfLink`, `Count`, `CountParam`, `IsCountOnly`, `SortBy`) now have `private init` setters.
- `WhenCreated`, `Includes`, `ReverseIncludes`, and `Elements` have been converted from public fields to properties with `private init` setters.
- The `Snapshot.Create` parameters `selflink` and `sortby` have been renamed to `selfLink` and `sortBy` respectively.
- The `Snapshot.CreateCountOnly` parameter `selflink` has been renamed to `selfLink`.

### Method and property signature changes
- `Validate.HasResourceType(IKey, ResourceType)` has been changed to `Validate.HasResourceType(IKey, string)`
- `SearchService` no longer implements `IServiceListener`; it now only implements `ISearchService`. Code that registered or resolved `SearchService` as `IServiceListener` must be updated.
- `SearchService` constructor no longer accepts `IIndexService`; the signature changed from `SearchService(ILocalhost, IFhirModel, IFhirIndex, IIndexService)` to `SearchService(ILocalhost, IFhirModel, IFhirIndex)`.
- `SparkSettings` has a new property `ExperimentalSettings Experimental { get; set; }` (default `new ExperimentalSettings()`).
- `StoreSettings` has a new property `IndexQueueSettings IndexQueue { get; set; }` (defaults to `new IndexQueueSettings()`).
- `List<Hl7.Fhir.Model.SearchParameter> IFhirModel.SearchParameters` has been changed to `IReadOnlyListList<Spark.Engine.Model.SearchParameter> IFhirModel.SearchParameters`
- `IEnumerable<Hl7.Fhir.Model.SearchParameter> IFhirModel.FindSearchParameters(Type)` has been changed to `List<Spark.Engine.Model.SearchParameter> IFhirModel.FindSearchParameters(Type)`
- `IEnumerable<Hl7.Fhir.Model.SearchParameter> IFhirModel.FindSearchParameters(string)` has been changed to `List<Spark.Engine.Model.SearchParameter> IFhirModel.FindSearchParameters(string)`
- `IEnumerable<Hl7.Fhir.Model.SearchParameter> IFhirModel.FindSearchParameters(Type, string)` has been changed to `List<Spark.Engine.Model.SearchParameter> IFhirModel.FindSearchParameters(Type, string)`
- `IEnumerable<Hl7.Fhir.Model.SearchParameter> IFhirModel.FindSearchParameters(string, string)` has been changed to `List<Spark.Engine.Model.SearchParameter> IFhirModel.FindSearchParameters(string, string)`
- `Task IFhirStore.AddAsync(Entry)` has been changed to `Task<Entry> IFhirStore.AddAsync(Entry)`

### Removed classes and interfaces
- `ResourceVisitor` (`Spark.Engine.Core`)
- `FhirPropertyIndex` (`Spark.Engine.Core`)
- `BinaryFormatter` (`Spark.Formatters`)
- `FhirContentNegotiator` (`Spark.Formatters`)
- `FhirMediaTypeFormatter` (`Spark.Formatters`)
- `HtmlFhirFormatter` (`Spark.Formatters`)
- `JsonFhirFormatter` (`Spark.Formatters`)
- `XmlFhirFormatter` (`Spark.Formatters`)
- `MediaTypeHandler` (`Spark.Handlers`)
- `XmlSignatureHelper` (`Spark.Engine.Auxiliary`)
- `IExceptionResponseMessageFactory` (`Spark.Engine.ExceptionHandling`)
- `NullExtensions` (`Spark.Search.Support`)
- `XmlNs` (`Spark.Search.Support`)
- `SparkModelInfo` (`Spark.Egine.Model`)
- `BsonIndexDocumentBuilder` (`Spark.Mongo.Search.Indexer`)

### Removed methods and extensions methods
- `AddFhirFormatters(this IServiceCollection, Action<MvcOptions>)` has been removed, use
  `AddFhirFormatters(this IServiceCollection, SparkSettings, Action<MvcOptions>)` instead.
- ctor `ResourceJsonInputFormatter()` has been removed, use ctor `ResourceJsonInputFormatter(FhirJsonParser, ArrayPool<char>)`
  instead.
- ctor `ResourceXmlInputFormatter()` has been removed, use ctor `ResourceJXmlInputFormatter(FhirXmlParser)` instead.
- `ToHttpResponseMessage(this OperationOutcome, ResourceFormat, HttpRequestMessage)` has been removed.
- `ToHttpResponseMessage(this OperationOutcome, ResourceFormat)` has been removed.
- `FhirService(IFhirServiceExtension[], IFhirResponseFactory, ITransfer, ICompositeServiceListener)` has been removed, use
  `FhirService(IFhirServiceExtension[], IFhirResponseFactory, ICompositeServiceListener)` instead.
- `IsRawBinaryPostOrPutRequest(this HttpRequestMessage)` has been removed.
- `IsRawBinaryRequest(this HttpRequestMessage, Type)` has been removed.
- `IsAcceptHeaderFhirMediaType(this HttpRequestMessage request)` has been removed.
- `TransferResourceIdIfRawBinary(this HttpRequestMessage, Resource, string)` has been removed.
- `IfMatchVersionId(this HttpRequestMessage)` has been removed.
- `AcquireHeaders(this HttpResponseMessage, FhirResponse)` has been removed.
- `GetPagingOffsetParameter(this HttpRequestMessage request)` has been removed.
- `AddMessage(this OperationOutcome, string)` has been removed.
- `AddMessage(this OperationOutcome, HttpStatusCode, string)` has been removed.
- `GetPrivateKey(this X509Certificate2)` has been removed.
- `GetMediaType(this HttpRequestMessage)` has been removed.
- `GetContentTypeHeaderValue(this HttpRequestMessage)` has been removed.
- `GetAcceptHeaderValue(this HttpRequestMessage)` has been removed.
- `FhirMediaType.GetMediaTypeHeaderValue(Type, ResourceFormat)` has been removed.
- `FhirMediaType.GetContentType(Type, ResourceFormat)` has been removed.
- `FhirMediaType.GetContentType(Type, ResourceFormat)` has been removed.
- `FhirMediaType.GetResourceFormat(string format)` has been removed.
- `FhirMediaType.Interpret(string)` has been removed.
- `StringExtensions.SplitNotInQuotes(string, char)` has been removed.
- `IFhirModel.GetResourceTypeForResourceName(string)` has been removed.
- `IFhirModel.GetResourceNameForResourceType(ResourceType)` has been removed.
- `IFhirModel.FindSearchParameters(ResourceType)` has been removed.
- `IFhirModel.FindSearchParameters(string)` has been removed.
- `IFhirModel.FindSearchParameter(ResourceType, string)` has been removed.
- `IFhirModel.FindCompartmentInfo(ResourceType)` has been removed.

### Replacement methods and extension methods
- `IFhirModel.FindSearchParameters(string)` has replaced `IFhirModel.FindSearchParameters(ResourceType)`.

### Changes to extension methods
- `AddFhirFacade(this IServiceCollection, Action<SparkOptions>)` now returns `IMvcBuilder` instead of `IMvcCoreBuilder`.
- `AddFhir(this IServiceCollection, SparkSettings, Action<MvcOptions>)` now returns `IMvcBuilder` instead of `IMvcCoreBuilder`. It also conditionally registers `IndexServiceListener` (default, `IndexingMode.Synchronous`) or `IndexQueueEnqueueListener` + `IndexWorker` (opt-in, `IndexingMode.Background`) based on `SparkSettings.Experimental.IndexingMode`.
- `AddFhirFormatters(this IServiceCollection, SparkSettings, Action<MvcOptions>` now returns `IMvcBuilder` instead of `IMvcCoreBuilder`.
- `AddMongoFhirStore(this IServiceCollection, StoreSettings)` now always registers `IndexQueueSettings` (sourced from `StoreSettings.IndexQueue`) and `IIndexQueue → MongoIndexQueue`.

### Namespace changes
- `Spark.Search.ChoiceValue` moved to `Spark.Engine.Search.ChoiceValue`
- `Spark.Search.CompositeValue` moved to `Spark.Engine.Search.CompositeValue`
- `Spark.Search.Criterium` moved to `Spark.Engine.Search.Criterium`
- `Spark.Search.DateTimeValue` moved to `Spark.Engine.Search.DateTimeValue`
- `Spark.Search.DateValue` moved to `Spark.Engine.Search.DateValue`
- `Spark.Search.Expression` moved to `Spark.Engine.Search.Expression`
- `Spark.Search.NumberValue` moved to `Spark.Engine.Search.NumberValue`
- `Spark.Search.QuantityValue` moved to `Spark.Engine.Search.QuantityValue`
- `Spark.Search.ReferenceValue` moved to `Spark.Engine.Search.ReferenceValue`
- `Spark.Search.StringValue` moved to `Spark.Engine.Search.StringValue`
- `Spark.Search.TokenValue` moved to `Spark.Engine.Search.TokenValue`
- `Spark.Search.UntypedValue` moved to `Spark.Engine.Search.UntypedValue`
- `Spark.Search.ValueExpression` moved to `Spark.Engine.Search.ValueExpression`
- `Spark.Core.Error` moved to `Spark.Engine.Core.Error`
- `Spark.Engine.Handlers.NetCore.FormatTypeHandler` moved to `Spark.Engine.Handlers.FormatTypeHandler`
- `Spark.Engine.Core.KeyExtensions` moved to `Spark.Engine.Extensions.KeyExtensions`
- `Spark.Filters.GZipCompressedContent` moved to `Spark.Engine.Filters.GZipCompressedContent`
- `Spark.Filters.GZipContent` moved to `Spark.Engine.Filters.GZipContent`
- `Spark.Core.IFhirIndex` moved to `Spark.Engine.Interfaces.IFhirIndex`
- `Spark.Core.IIdentityGenerator` moved to `Spark.Engine.Interfaces.IIdentityGenerator`
- `Spark.Search.Support.IPostitionInfo` moved to `Spark.Engine.Search.Support.IPositionInfo`
- `Spark.Service.Export` moved to `Spark.Engine.Service.Export`
- `Spark.Service.Import` moved to `Spark.Engine.Service.Import`
- `Spark.Service.IServiceListener` moved to `Spark.Engine.Service.IServiceListener`
- `Spark.Service.ITransfer` moved to `Spark.Engine.Service.ITransfer`
- `Spark.Service.Mapper<TKey, Tvalue>` moved to `Spark.Engine.Service.Mapper<TKey, Tvalue>`
- `Spark.Service.ServiceListener` moved to `Spark.Engine.Service.ServiceListener`
- `Spark.Service.Transfer` moved to `Spark.Engine.Service.Transfer`
- `Spark.Service.Validate` moved to `Spark.Engine.Service.Validate`
- `Spark.Core.XDocumentExtensions` moved to `Spark.Engine.Extensions.XDocumentExtensions`
- `Spark.Engine.Storage.ExtendableWith<T>` moved to `Spark.Engine.Store.ExtendableWith<T>`
- `Spark.Search.Support.StringExtensions` moved to `Spark.Engine.Search.Support.StringExtensions`
