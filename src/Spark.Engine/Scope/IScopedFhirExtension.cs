using Spark.Engine.Interfaces;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Scope
{
    public interface IScopedFhirExtension<T> : IFhirStoreExtension
    {
        T Scope { set; }
    }
}