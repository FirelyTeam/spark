using System.Threading.Tasks;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ISearchService : IFhirServiceExtension
    {
        Task<Snapshot> GetSnapshot(string type, SearchParams searchCommand);
        Task<Snapshot> GetSnapshotForEverything(IKey key);
        Task<IKey> FindSingle(string type, SearchParams searchCommand);
        Task<IKey> FindSingleOrDefault(string type, SearchParams searchCommand);
        Task<SearchResults> GetSearchResults(string type, SearchParams searchCommand);
    }
}
