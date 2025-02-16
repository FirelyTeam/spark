## Migrate from 2.0 to 3.0

### Target Frameworks
We now target `netstandard2.1` and `net472`, allowing Spark to run on .NET 8.0 and later, as well as .NET Framework
4.7.2 and later.

### New classes and interfaces
- `FhirResponse&lt;T&gt;` wraps a `FhirResponse`, with the generic parameter representing the FHIR resource type.

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

### Removed methods
- `AddFhirFormatters(this IServiceCollection, Action<MvcOptions>)` has been removed, use
  `AddFhirFormatters(this IServiceCollection, SparkSettings, Action<MvcOptions>)` instead.
- ctor `ResourceJsonInputFormatter()` has been removed, use ctor `ResourceJsonInputFormatter(FhirJsonParser, ArrayPool<char>)`
  instead.
- ctor `ResourceXmlInputFormatter()` has been removed, use ctor `ResourceJXmlInputFormatter(FhirXmlParser)` instead.
- `ToHttpResponseMessage(this OperationOutcome, ResourceFormat, HttpRequestMessage)` has been removed.
- `ToHttpResponseMessage(this OperationOutcome, ResourceFormat)` has been removed.
- `FhirService(IFhirServiceExtension[], IFhirResponseFactory, ITransfer, ICompositeServiceListener)` has been removed, use
  `FhirService(IFhirServiceExtension[], IFhirResponseFactory, ICompositeServiceListener)` instead.

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