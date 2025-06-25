## Migrate from v2 to v3

### Target Frameworks
We now target `net8` and `net9` and have removed the `netstandard2.0` and `net472` target.

### New classes and interfaces
- `FhirResponse<T>` wraps a `FhirResponse`, with the generic parameter representing the FHIR resource type.

### IFhirService, FhirServiceBase and FhirService changes
- New generic methods:
  - `Task<FhirResponse<T>> ReadAsync<T>(IKey, ConditionalHeaderParameters) where T : Resource`
  - `Task<FhirResponse<T>> VersionReadAsync<T>(IKey, ConditionalHeaderParameters) where T : Resource`

### IFhirResponseFactory and FhirResponseFactory changes
- New generic methods:
  - `FhirResponse<T> GetFhirResponse<T>(Entry entry, IKey key = null, IEnumerable<object> parameters = null) where T : Resource`
  - `FhirResponse<T> GetFhirResponse<T>(Entry entry, IKey key = null, params object[] parameters) where T : Resource`

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

### Changes to extension methods
- `AddFhirFacade(this IServiceCollection, Action<SparkOptions>)` now returns `IMvcBuilder` instead of `IMvcCoreBuilder`.
- `AddFhir(this IServiceCollection, SparkSettings, Action<MvcOptions>)` now returns `IMvcBuilder` instead of `IMvcCoreBuilder`.
- `AddFhirFormatters(this IServiceCollection, SparkSettings, Action<MvcOptions>` now returns `IMvcBuilder` instead of `IMvcCoreBuilder`.

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
