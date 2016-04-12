using Hl7.Fhir.Model;
using Spark.Core;

namespace Spark.Engine.Interfaces
{
    public interface IScopedFhirStore<T> : IFhirStore, IGenerator
    {
         T Scope { get; set; }
    }

  
}   