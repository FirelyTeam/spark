using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.FhirResponseFactory
{
    public interface IFhirResponseFactory
    {
        FhirResponse GetFhirResponse(Entry entry, Key key = null, params object[] parameters);
        FhirResponse GetFhirResponse(Entry entry, Key key = null, IEnumerable<object> parameters = null);
        FhirResponse GetMetadataResponse(Entry entry, Key key = null);

        FhirResponse GetFhirResponse(IList<Entry> interactions, Bundle.BundleType bundleType);

        FhirResponse GetFhirResponse(Bundle bundle);
    }
}