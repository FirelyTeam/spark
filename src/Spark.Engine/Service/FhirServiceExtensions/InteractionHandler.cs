using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IInteractionHandler
    {
        Task<FhirResponse> HandleInteraction(Entry interaction);
    }
}
