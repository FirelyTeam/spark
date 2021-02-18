using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Service;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class SearchService : ISearchService, IServiceListener
    {
        private readonly IFhirModel fhirModel;
        private readonly ILocalhost localhost;
        private IIndexService indexService;
        private IFhirIndex fhirIndex;

        public SearchService(ILocalhost localhost, IFhirModel fhirModel, IFhirIndex fhirIndex, IIndexService indexService)
        {
            this.fhirModel = fhirModel;
            this.localhost = localhost;
            this.indexService = indexService;
            this.fhirIndex = fhirIndex;
        }

        [Obsolete("Use GetSnapshotAsync(string, SearchParams) instead")]
        public Snapshot GetSnapshot(string type, SearchParams searchCommand)
        {
            return Task.Run(() => GetSnapshotAsync(type, searchCommand)).GetAwaiter().GetResult();
        }

        [Obsolete("Use GetSnapshotForEverythingAsync(IKey) instead")]
        public Snapshot GetSnapshotForEverything(IKey key)
        {
            return Task.Run(() => GetSnapshotForEverythingAsync(key)).GetAwaiter().GetResult();
        }

        [Obsolete("Use FindSingleAsync(string, SearchParams) instead")]
        public IKey FindSingle(string type, SearchParams searchCommand)
        {
            return Task.Run(() => FindSingleAsync(type, searchCommand)).GetAwaiter().GetResult();
        }

        [Obsolete("Use Async method version instead")]
        public IKey FindSingleOrDefault(string type, SearchParams searchCommand)
        {
            return Task.Run(() => FindSingleOrDefaultAsync(type, searchCommand)).GetAwaiter().GetResult();
        }

        [Obsolete("Use Async method version instead")]
        public SearchResults GetSearchResults(string type, SearchParams searchCommand)
        {
            return Task.Run(() => GetSearchResultsAsync(type, searchCommand)).GetAwaiter().GetResult();
        }

        public async Task<Snapshot> GetSnapshotAsync(string type, SearchParams searchCommand)
        {
            Validate.TypeName(type);
            SearchResults results = await fhirIndex.SearchAsync(type, searchCommand).ConfigureAwait(false);

            if (results.HasErrors)
            {
                throw new SparkException(HttpStatusCode.BadRequest, results.Outcome);
            }

            UriBuilder builder = new UriBuilder(localhost.Uri(type));
            builder.Query = results.UsedParameters;
            Uri link = builder.Uri;

            return CreateSnapshot(link, results, searchCommand);
        }

        public async Task<Snapshot> GetSnapshotForEverythingAsync(IKey key)
        {
            var searchCommand = new SearchParams();
            if (string.IsNullOrEmpty(key.ResourceId) == false)
            {
                searchCommand.Add("_id", key.ResourceId);
            }
            var compartment = fhirModel.FindCompartmentInfo(key.TypeName);
            if (compartment != null)
            {
                foreach (var ri in compartment.ReverseIncludes)
                {
                    searchCommand.RevInclude.Add((ri, IncludeModifier.None));
                }
            }

            return await GetSnapshotAsync(key.TypeName, searchCommand).ConfigureAwait(false);
        }

        private Snapshot CreateSnapshot(Uri selflink, IEnumerable<string> keys, SearchParams searchCommand)
        {
            string sort = GetFirstSort(searchCommand);

            int? count = searchCommand.Count;
            if (count.HasValue)
            {
                //TODO: should we change count?
                //count = Math.Min(searchCommand.Count.Value, MAX_PAGE_SIZE);
                selflink = selflink.AddParam(SearchParams.SEARCH_PARAM_COUNT, new string[] { count.ToString() });
            }

            if (searchCommand.Sort.Any())
            {
                foreach (var tuple in searchCommand.Sort)
                {
                    selflink = selflink.AddParam(SearchParams.SEARCH_PARAM_SORT,
                        string.Format("{0}:{1}", tuple.Item1, tuple.Item2 == SortOrder.Ascending ? "asc" : "desc"));
                }
            }

            if (searchCommand.Include.Any())
            {
                selflink = selflink.AddParam(SearchParams.SEARCH_PARAM_INCLUDE, searchCommand.Include.Select(inc => inc.Item1).ToArray());
            }

            if (searchCommand.RevInclude.Any())
            {
                selflink = selflink.AddParam(SearchParams.SEARCH_PARAM_REVINCLUDE, searchCommand.RevInclude.Select(inc => inc.Item1).ToArray());
            }

            return Snapshot.Create(Bundle.BundleType.Searchset, selflink, keys, sort, count, searchCommand.Include.Select(inc => inc.Item1).ToList(), 
                searchCommand.RevInclude.Select(inc => inc.Item1).ToList());
        }

        private static string GetFirstSort(SearchParams searchCommand)
        {
            string firstSort = null;
            if (searchCommand.Sort != null && searchCommand.Sort.Any())
            {
                firstSort = searchCommand.Sort[0].Item1; //TODO: Support sortorder and multiple sort arguments.
            }
            return firstSort;
        }

        public async Task<IKey> FindSingleAsync(string type, SearchParams searchCommand)
        {
            return Key.ParseOperationPath((await GetSearchResultsAsync(type, searchCommand).ConfigureAwait(false)).Single());
        }

        public async Task<IKey> FindSingleOrDefaultAsync(string type, SearchParams searchCommand)
        {
            string value = (await GetSearchResultsAsync(type, searchCommand).ConfigureAwait(false)).SingleOrDefault();
            return  value != null? Key.ParseOperationPath(value) : null;
        }

        public async Task<SearchResults> GetSearchResultsAsync(string type, SearchParams searchCommand)
        {
            Validate.TypeName(type);
            SearchResults results = await fhirIndex.SearchAsync(type, searchCommand).ConfigureAwait(false);

            if (results.HasErrors)
            {
                throw new SparkException(HttpStatusCode.BadRequest, results.Outcome);
            }

            return results;
        }

        public void Inform(Uri location, Entry interaction)
        {
            indexService.Process(interaction);
        }

        public async Task InformAsync(Uri location, Entry interaction)
        {
            await indexService.ProcessAsync(interaction).ConfigureAwait(false);
        }
    }
}