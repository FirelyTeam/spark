using System;
using System.Threading.Tasks;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ISearchService : IFhirServiceExtension
    {
        [Obsolete("Use GetSnapshotAsync(string, SearchParams) instead")]
        Snapshot GetSnapshot(string type, SearchParams searchCommand);
        [Obsolete("Use GetSnapshotForEverythingAsync(IKey) instead")]
        Snapshot GetSnapshotForEverything(IKey key);
        [Obsolete("Use FindSingleAsync(string, SearchParams) instead")]
        IKey FindSingle(string type, SearchParams searchCommand);
        [Obsolete("Use FindSingleOrDefaultAsync(string, SearchParams) instead")]
        IKey FindSingleOrDefault(string type, SearchParams searchCommand);
        [Obsolete("Use GetSnapshotAsync(string, SearchParams) instead")]
        SearchResults GetSearchResults(string type, SearchParams searchCommand);
        Task<Snapshot> GetSnapshotAsync(string type, SearchParams searchCommand);
        Task<Snapshot> GetSnapshotForEverythingAsync(IKey key);
        Task<IKey> FindSingleAsync(string type, SearchParams searchCommand);
        Task<IKey> FindSingleOrDefaultAsync(string type, SearchParams searchCommand);
        Task<SearchResults> GetSearchResultsAsync(string type, SearchParams searchCommand);
    }
}