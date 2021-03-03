using System;
using System.Threading.Tasks;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ISearchService : IFhirServiceExtension
    {
        [Obsolete("Use Async method version instead")]
        Snapshot GetSnapshot(string type, SearchParams searchCommand);

        [Obsolete("Use Async method version instead")]
        Snapshot GetSnapshotForEverything(IKey key);

        [Obsolete("Use Async method version instead")]
        IKey FindSingle(string type, SearchParams searchCommand);

        [Obsolete("Use Async method version instead")]
        IKey FindSingleOrDefault(string type, SearchParams searchCommand);

        [Obsolete("Use Async method version instead")]
        SearchResults GetSearchResults(string type, SearchParams searchCommand);

        Task<Snapshot> GetSnapshotAsync(string type, SearchParams searchCommand);

        Task<Snapshot> GetSnapshotForEverythingAsync(IKey key);

        Task<IKey> FindSingleAsync(string type, SearchParams searchCommand);

        Task<IKey> FindSingleOrDefaultAsync(string type, SearchParams searchCommand);

        Task<SearchResults> GetSearchResultsAsync(string type, SearchParams searchCommand);
    }
}