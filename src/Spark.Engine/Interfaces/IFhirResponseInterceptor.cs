using Spark.Engine.Core;

namespace Spark.Engine.Interfaces
{
    public interface IFhirResponseInterceptor
    {
        FhirResponse GetFhirResponse(Interaction interaction, object input);

        bool CanHandle(object input);
    }
}