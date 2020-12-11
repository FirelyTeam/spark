using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    using System.Threading.Tasks;

    public interface IInteractionHandler
    {
        Task<FhirResponse> HandleInteraction(Entry interaction);
    }
}