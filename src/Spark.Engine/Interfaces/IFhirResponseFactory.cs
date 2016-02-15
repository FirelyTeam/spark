using System.Collections.Generic;
using Spark.Engine.Core;

namespace Spark.Engine.Interfaces
{
    public interface IFhirResponseFactory
    {
        FhirResponse GetFhirResponse(Key key, IEnumerable<object> parameters =  null);
        FhirResponse GetFhirResponse(Entry entry, IEnumerable<object> parameters = null);
        FhirResponse GetFhirResponse(Key key, params object[] parameters);
        FhirResponse GetFhirResponse(Entry entry, params object[] parameters);
    }
}