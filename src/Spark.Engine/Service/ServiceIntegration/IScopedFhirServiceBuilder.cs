using Spark.Service;

namespace Spark.Engine.Service.ServiceIntegration
{
    public interface IScopedFhirServiceBuilder<in T>
    {
        IFhirService WithScope(T scope);
    }
}