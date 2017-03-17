using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IInteractionHandler
    {
        FhirResponse HandleInteraction(Entry interaction);
    }
}