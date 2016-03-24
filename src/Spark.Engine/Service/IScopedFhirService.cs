using Spark.Service;

namespace Spark.Engine.Service
{
    public interface IScopedFhirService<in T> : IFhirService
    {
        IFhirService WithScope(T scope);
    }
}