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

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class SearchService : ISearchService, IServiceListener
    {
        private readonly IFhirModel fhirModel;
        private readonly ILocalhost localhost;
        private IIndexService indexService;
        private IFhirIndex fhirIndex;

        public SearchService(ILocalhost localhost, IFhirModel fhirModel, IFhirIndex fhirIndex, IIndexService indexService = null)
        {
            this.fhirModel = fhirModel;
            this.localhost = localhost;
            this.indexService = indexService;
            this.fhirIndex = fhirIndex;
        }

        public async Task<Snapshot> GetSnapshot(string type, SearchParams searchCommand)
        {
            Validate.TypeName(type);
            SearchResults results = await fhirIndex.Search(type, searchCommand);

            if (results.HasErrors)
            {
                throw new SparkException(HttpStatusCode.BadRequest, results.Outcome);
            }

            UriBuilder builder = new UriBuilder(localhost.Uri(type));
            builder.Query = results.UsedParameters;
            Uri link = builder.Uri;

            Snapshot snapshot = CreateSnapshot(link, results, searchCommand);
            return snapshot;
        }

        public Task<Snapshot> GetSnapshotForEverything(IKey key)
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
                    searchCommand.RevInclude.Add(ri);
                }
            }

            return GetSnapshot(key.TypeName, searchCommand);
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
                foreach (Tuple<string, SortOrder> tuple in searchCommand.Sort)
                {
                    selflink = selflink.AddParam(SearchParams.SEARCH_PARAM_SORT,
                        string.Format("{0}:{1}", tuple.Item1, tuple.Item2 == SortOrder.Ascending ? "asc" : "desc"));
                }
            }

            if (searchCommand.Include.Any())
            {
                selflink = selflink.AddParam(SearchParams.SEARCH_PARAM_INCLUDE, searchCommand.Include.ToArray());
            }

            if (searchCommand.RevInclude.Any())
            {
                selflink = selflink.AddParam(SearchParams.SEARCH_PARAM_REVINCLUDE, searchCommand.RevInclude.ToArray());
            }

            return Snapshot.Create(Bundle.BundleType.Searchset, selflink, keys, sort, count, searchCommand.Include, searchCommand.RevInclude);
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

        public async Task<IKey> FindSingle(string type, SearchParams searchCommand)
        {
            var results = await GetSearchResults(type, searchCommand);
            var value = results.Single();
            return Key.ParseOperationPath(value);
        }

        public async Task<IKey> FindSingleOrDefault(string type, SearchParams searchCommand)
        {
            var results = await GetSearchResults(type, searchCommand);
            var value = results.SingleOrDefault();
            return value != null ? Key.ParseOperationPath(value) : null;
        }

        public async Task<SearchResults> GetSearchResults(string type, SearchParams searchCommand)
        {
            Validate.TypeName(type);
            SearchResults results = await fhirIndex.Search(type, searchCommand);

            if (results.HasErrors)
            {
                throw new SparkException(HttpStatusCode.BadRequest, results.Outcome);
            }

            return results;
        }

        public async System.Threading.Tasks.Task Inform(Uri location, Entry interaction)
        {
            if (indexService != null)
            {
                await indexService.Process(interaction);
            }
            else if (fhirIndex != null)
            {
                //TODO: If IndexService is working correctly, remove the reference to fhirIndex.
                await fhirIndex.Process(interaction);
            }
        }
    }
}
