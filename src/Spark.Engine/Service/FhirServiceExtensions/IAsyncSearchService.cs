using System.Threading.Tasks;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IAsyncSearchService : IFhirServiceExtension
    {
        Task<Snapshot> GetSnapshotAsync(string type, SearchParams searchCommand);

        Task<Snapshot> GetSnapshotForEverythingAsync(IKey key);

        Task<IKey> FindSingleAsync(string type, SearchParams searchCommand);

        Task<IKey> FindSingleOrDefaultAsync(string type, SearchParams searchCommand);

        Task<SearchResults> GetSearchResultsAsync(string type, SearchParams searchCommand);
    }
}
