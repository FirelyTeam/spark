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

