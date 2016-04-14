using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Scope
{
    public interface IScopedFhirStore<T> : IFhirStore
    {
         T Scope { get; set; }
    }
}   