## Migrate from v2 to v3

### New NuGet package: `Spark.Engine`

A new NuGet package `Spark.Engine` has been introduced, this contains all non-version-specific code and is the shared
library for the satellite assemblies `Spark.Engine.R4` and `Spark.Engine.STU3`. As a user of the library you should
continue to reference the version-specific assembly `Spark.Engine.R4` or `Spark.Engine.STU3`.

### Target Frameworks
We now target `net8.0`, `net9.0`, and `net10.0`. `netstandard2.0` and `net472` targets have been removed.

### New classes and interfaces
- `SearchParameterComponent` (`Spark.Engine.Model`) — record with `string Definition` and `string Expression` properties that represents a component of a composite search parameter. Exposed via the new `Component` property on `Spark.Engine.Model.SearchParameter`.
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
- `Spark.Engine.Model.SearchParameter.Path` (`string[]`) — new property that exposes the XPath expressions for the parameter's target elements (sourced from `ModelInfo.SearchParamDefinition.Path`).
- `IFhirModel.SupportedResources` (`IReadOnlyList<string>`) — new member; returns all supported FHIR resource type names.
- `IFhirModel.FhirRelease` (`string`) — new member; returns the FHIR version string (e.g. `"4.0.1"`). Previously on `SparkSettings`.
- `IFhirModel.GetModelInspector()` (`ModelInspector`) — new member; returns the FHIR model inspector for class mapping lookups.
- `IFhirModel.GetTypeForFhirType(string)` (`Type`) — new member; maps a FHIR type name to its C# type.
- `IFhirModel.GetFhirTypeNameForType(Type)` (`string`) — new member; maps a C# type to its FHIR type name.

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
- `SearchService` no longer implements `IServiceListener`, it now only implements `ISearchService`. Code that registered or resolved `SearchService` as `IServiceListener` must be updated.
- `SearchService` constructor no longer accepts `IIndexService`; the signature changed from `SearchService(ILocalhost, IFhirModel, IFhirIndex, IIndexService)` to `SearchService(ILocalhost, IFhirModel, IFhirIndex)`.
- `SparkSettings` has a new property `ExperimentalSettings Experimental { get; set; }` (default `new ExperimentalSettings()`).
- `StoreSettings` has a new property `IndexQueueSettings IndexQueue { get; set; }` (defaults to `new IndexQueueSettings()`).
- `List<Hl7.Fhir.Model.SearchParameter> IFhirModel.SearchParameters` has been changed to `List<Spark.Engine.Model.SearchParameter> IFhirModel.SearchParameters`
- `IEnumerable<Hl7.Fhir.Model.SearchParameter> IFhirModel.FindSearchParameters(Type)` has been changed to `IEnumerable<Spark.Engine.Model.SearchParameter> IFhirModel.FindSearchParameters(Type)`.
- `IEnumerable<Hl7.Fhir.Model.SearchParameter> IFhirModel.FindSearchParameters(string)` has been changed to `IEnumerable<Spark.Engine.Model.SearchParameter> IFhirModel.FindSearchParameters(string)`.
- `Hl7.Fhir.Model.SearchParameter IFhirModel.FindSearchParameter(Type, string)` has been changed to `Spark.Engine.Model.SearchParameter IFhirModel.FindSearchParameter(Type, string)`.
- `Hl7.Fhir.Model.SearchParameter IFhirModel.FindSearchParameter(string, string)` has been changed to `Spark.Engine.Model.SearchParameter IFhirModel.FindSearchParameter(string, string)`.
- `Task IFhirStore.AddAsync(Entry)` has been changed to `Task<Entry> IFhirStore.AddAsync(Entry)`
- `Criterium.SearchParameters` has been changed from `List<Hl7.Fhir.Model.ModelInfo.SearchParamDefinition>` to `List<Spark.Engine.Model.SearchParameter>`
- `Criterium.Parse(string, string, string)` has been changed to `Criterium.Parse(IReadOnlyList<SearchParameter>, string, string, string)`.
- `DefinitionsFactory.Generate(IEnumerable<Hl7.Fhir.Model.ModelInfo.SearchParamDefinition>)` has been changed to `DefinitionsFactory.Generate(IFhirModel)`
- `DefinitionsFactory.CreateDefinition(SearchParameter)` has been changed to `DefinitionsFactory.CreateDefinition(IFhirModel, SearchParameter)`
- `SparkSettings.FhirRelease` has been removed; use `IFhirModel.FhirRelease` instead.
- `ElementQuery(params string[] paths)` has been changed to `ElementQuery(IFhirModel fhirModel, params string[] paths)`
- `ElementQuery(string path)` has been changed to `ElementQuery(IFhirModel fhirModel, string path)`
- `ResourceResolver()` has been changed to `ResourceResolver(IReadOnlyList<string> supportedResources)`
- `SnapshotPaginationService(IFhirIndex, IFhirStore, ITransfer, ILocalhost, ISnapshotPaginationCalculator, Snapshot)` has been changed to accept a trailing `IFhirModel fhirModel` parameter.
- `Validate.TypeName(string)` has been changed to `Validate.TypeName(string, IReadOnlyList<string> supportedResources = null)`
- `Validate.Key(IKey)` has been changed to `Validate.Key(IKey, IReadOnlyList<string> supportedResources = null)`
- `CriteriaMongoExtensions.GetTargetedReferenceTypes(this Criterium, string)` has been changed to `GetTargetedReferenceTypes(this Criterium, IReadOnlyList<SearchParameter>, string)`
- `FhirService(IFhirServiceExtension[], IFhirResponseFactory, ICompositeServiceListener)` has been changed to `FhirService(IFhirModel, IFhirServiceExtension[], IFhirResponseFactory, ICompositeServiceListener)`
- `FhirServiceBase(IFhirServiceExtension[], IFhirResponseFactory, ICompositeServiceListener)` has been changed to `FhirServiceBase(IFhirModel, IFhirServiceExtension[], IFhirResponseFactory, ICompositeServiceListener)`

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
- `Spark.Engine.Model.SearchParameter.OriginalDefinition` property has been removed; use `Spark.Engine.Model.SearchParameter.Component` (`SearchParameterComponent[]`) to access composite sub-parameter definitions.
- `SearchParamDefinitionExtensions` (`Spark.Engine.Extensions`) — `CanHaveOperatorPrefix` is now an extension on `Spark.Engine.Model.SearchParameter` in `Spark.Engine.Extensions.SearchParameterExtensions`.
- `Modifier` (`Spark.Engine.Search.Model`) — removed (dead code).
- `ActualModifier` — removed (dead code).

### Removed methods and extensions methods
- `AddFhirFormatters(this IServiceCollection, Action<MvcOptions>)` has been removed, use
  `AddFhirFormatters(this IServiceCollection, SparkSettings, Action<MvcOptions>)` instead.
- ctor `ResourceJsonInputFormatter()` has been removed, use ctor `ResourceJsonInputFormatter(FhirJsonParser, ArrayPool<char>)`
  instead.
- ctor `ResourceXmlInputFormatter()` has been removed, use ctor `ResourceJXmlInputFormatter(FhirXmlParser)` instead.
- `ToHttpResponseMessage(this OperationOutcome, ResourceFormat, HttpRequestMessage)` has been removed.
- `ToHttpResponseMessage(this OperationOutcome, ResourceFormat)` has been removed.
- `FhirService(IFhirServiceExtension[], IFhirResponseFactory, ITransfer, ICompositeServiceListener)` has been removed, use
  `FhirService(IFhirMode, IFhirServiceExtension[], IFhirResponseFactory, ICompositeServiceListener)` instead.
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
- `Criterium.Parse(string)` has been removed.
- `SparkBsonHelper.CreateDocument(Resource)` has been removed.
- `SparkBsonHelper.ToBsonReferenceKey(IKey)` has been removed.
- `SparkBsonHelper.ToBsonDocument(Entry)` has been removed.
- `SparkBsonHelper.ParseResource(BsonDocument)` has been removed.
- `SparkBsonHelper.ExtractMetadata(BsonDocument)` has been removed.
- `SparkBsonHelper.ToEntry(BsonDocument, bool)` has been removed.
- `SparkBsonHelper.ToEntries(IEnumerable<BsonDocument>, bool)` has been removed.
- `SparkBsonHelper.GetVersionDate(BsonDocument)` has been removed.
- `SparkBsonHelper.AddVersionDate(Entry, DateTime)` has been removed.
- `SparkBsonHelper.RemoveMetadata(BsonDocument)` has been removed.
- `SparkBsonHelper.AddMetaData(BsonDocument, Entry)` has been removed.
- `SparkBsonHelper.AddMetaData(BsonDocument, IKey, Resource)` has been removed.
- `SparkBsonHelper.GetKey(BsonDocument)` has been removed.
- `SparkBsonHelper.TransferMetadata(BsonDocument, BsonDocument)` has been removed.
- `EntryExtensions.GetReferences(this Resource, string)` has been removed.
- `EntryExtensions.GetReferences(this IEnumerable<Resource>, string)` has been removed.
- `EntryExtensions.GetReferences(this IEnumerable<Resource>, IEnumerable<string>)` has been removed.
- `EntryExtensions.SupplementBase(this Entry, string)` has been removed.
- `EntryExtensions.SupplementBase(this Entry, Uri)` has been removed.
- `EntryExtensions.ExtractKey(this ILocalhost, Bundle.EntryComponent)` has been removed.
- `EntryExtensions.ExtrapolateMethod(this ILocalhost, Bundle.EntryComponent, IKey)` has been removed.
- `EntryExtensions.ToInteraction(this ILocalhost, Bundle.EntryComponent)` has been removed.
- `EntryExtensions.TranslateToSparseEntry(this Entry, FhirResponse)` has been removed.
- `EntryExtensions.ToTransactionEntry(this Entry)` has been removed.
- `EntryExtensions.HasResource(this Entry)` has been removed.
- `EntryExtensions.IsDeleted(this Entry)` has been removed.
- `EntryExtensions.Append(this IList<Entry>, IList<Entry>)` has been removed.
- `EntryExtensions.AppendDistinct(this IList<Entry>, IList<Entry>)` has been removed.
- `EntryExtensions.GetResources(this IEnumerable<Entry>)` has been removed.
- `EntryExtensions.IsValidResourcePath(string, Resource)` has been removed.
- `EntryExtensions.IsValidResourcePath(string, Resource)` has been removed.
- `IServiceCollectionExtensions.AddCustomSearchParameters(this IServiceCollection, IEnumerable<SearchParameter>)` has been
  removed, set these via `SparkSettings.CustomSearchParameters` instead.

### Replacement methods and extension methods
- `IFhirModel.FindSearchParameters(string)` has replaced `IFhirModel.FindSearchParameters(ResourceType)`.

### Changes to extension methods
- `AddFhirFacade(this IServiceCollection, Action<SparkOptions>)` now returns `IMvcBuilder` instead of `IMvcCoreBuilder`.
- `AddFhir(this IServiceCollection, SparkSettings, Action<MvcOptions>)` now returns `IMvcBuilder` instead of `IMvcCoreBuilder`. It also conditionally registers `IndexServiceListener` (default, `IndexingMode.Synchronous`) or `IndexQueueEnqueueListener` + `IndexWorker` (opt-in, `IndexingMode.Background`) based on `SparkSettings.Experimental.IndexingMode`. It no longer registers `IFhirModel` or `CapabilityStatementService`; use `AddFhirR4()` instead.
- `AddFhirR4(this IServiceCollection, SparkSettings, Action<MvcOptions>)` (new, in `Spark.Engine.R4.Extensions`) — replaces direct calls to `AddFhir()`. Registers `IFhirModel`, `CapabilityStatementService`, and all `IFhirServiceExtension` implementations including the capability statement service, then delegates to `AddFhir()`.
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

### Types moved to `Spark.Engine.R4`

The following types retain their namespaces but have moved to the `Spark.Engine.R4`
assembly/package. Add a reference to `Spark.Engine.R4` if you use them directly:

- `Spark.Engine.Core.FhirModel`
- `Spark.Engine.Core.CapabilityStatementBuilder`
- `Spark.Engine.Core.MessagingComponentBuilder`
- `Spark.Engine.Core.ResourceComponentBuilder`
- `Spark.Engine.Core.RestComponentBuilder`
- `Spark.Engine.Service.FhirServiceExtensions.ConformanceBuilder`
- `Spark.Engine.Service.FhirServiceExtensions.ConformanceService`
