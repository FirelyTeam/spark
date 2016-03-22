using Spark.Engine.Core;

namespace Spark.Engine.Interfaces
{
    public interface IFhirResponseInterceptor
    {
        FhirResponse GetFhirResponse(Entry entry, object input);

        bool CanHandle(object input);
    }
}