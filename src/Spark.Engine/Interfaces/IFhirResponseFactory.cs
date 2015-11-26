using System.Collections.Generic;
using Spark.Engine.Core;

namespace Spark.Engine.Interfaces
{
    public interface IFhirResponseFactory
    {
        FhirResponse GetFhirResponse(Key key, IEnumerable<object> parameters =  null);
        FhirResponse GetFhirResponse(Interaction interaction, IEnumerable<object> parameters = null);
        FhirResponse GetFhirResponse(Key key, params object[] parameters);
        FhirResponse GetFhirResponse(Interaction interaction, params object[] parameters);
    }
}