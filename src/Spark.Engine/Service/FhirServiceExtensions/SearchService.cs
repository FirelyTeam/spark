/* 
 * Copyright (c) 2016-2018, Firely (info@fire.ly)
 * Copyright (c) 2019-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

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
        private readonly IFhirModel _fhirModel;
        private readonly ILocalhost _localhost;
        private IIndexService _indexService;
        private IFhirIndex _fhirIndex;

        public SearchService(ILocalhost localhost, IFhirModel fhirModel, IFhirIndex fhirIndex, IIndexService indexService)
        {
            _fhirModel = fhirModel;
            _localhost = localhost;
            _indexService = indexService;
            _fhirIndex = fhirIndex;
        }

        public async Task<Snapshot> GetSnapshotAsync(string type, SearchParams searchCommand)
        {
            Validate.TypeName(type);
            SearchResults results = await _fhirIndex.SearchAsync(type, searchCommand).ConfigureAwait(false);

            if (results.HasErrors)
            {
                throw new SparkException(HttpStatusCode.BadRequest, results.Outcome);
            }

            UriBuilder builder = new UriBuilder(_localhost.Uri(type))
            {
                Query = results.UsedParameters
            };
            Uri link = builder.Uri;

            return CreateSnapshot(type, link, results, searchCommand);
        }

        public async Task<Snapshot> GetSnapshotForEverythingAsync(IKey key)
        {
            var searchCommand = new SearchParams();
            if (string.IsNullOrEmpty(key.ResourceId) == false)
            {
                searchCommand.Add("_id", key.ResourceId);
            }
            var compartment = _fhirModel.FindCompartmentInfo(key.TypeName);
            if (compartment != null)
            {
                foreach (var ri in compartment.ReverseIncludes)
                {
                    searchCommand.RevInclude.Add((ri, IncludeModifier.None));
                }
            }

            return await GetSnapshotAsync(key.TypeName, searchCommand).ConfigureAwait(false);
        }

        public async Task<IKey> FindSingleAsync(string type, SearchParams searchCommand)
        {
            return Key.ParseOperationPath((await GetSearchResultsAsync(type, searchCommand).ConfigureAwait(false)).Single());
        }

        public async Task<IKey> FindSingleOrDefaultAsync(string type, SearchParams searchCommand)
        {
            string value = (await GetSearchResultsAsync(type, searchCommand).ConfigureAwait(false)).SingleOrDefault();
            return value != null ? Key.ParseOperationPath(value) : null;
        }

        public async Task<SearchResults> GetSearchResultsAsync(string type, SearchParams searchCommand)
        {
            Validate.TypeName(type);
            SearchResults results = await _fhirIndex.SearchAsync(type, searchCommand).ConfigureAwait(false);

            return results.HasErrors ? throw new SparkException(HttpStatusCode.BadRequest, results.Outcome) : results;
        }

        private Snapshot CreateSnapshot(string type, Uri selflink, IEnumerable<string> keys, SearchParams searchCommand)
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

            // add mandatory and modifier elements
            if (searchCommand.Elements != null && searchCommand.Elements.Any())
            {
                // TODO: Refactor in the next version.
                var classMapping = ModelInfo.ModelInspector.FindClassMapping(type);
                if (classMapping != null)
                {
                    foreach (var propertyMapping in classMapping.PropertyMappings)
                    {
                        if ((propertyMapping.IsModifier || propertyMapping.IsMandatoryElement)
                            && !searchCommand.Elements.Contains(propertyMapping.Name))
                        {
                            searchCommand.Elements.Add(propertyMapping.Name);
                        }
                    }
                }
            }

            return Snapshot.Create(Bundle.BundleType.Searchset, selflink, keys.ToList(), sort, count, searchCommand.Include.Select(inc => inc.Item1).ToList(),
                searchCommand.RevInclude.Select(inc => inc.Item1).ToList(), searchCommand.Elements);
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

        public async Task InformAsync(Uri location, Entry interaction)
        {
            await _indexService.ProcessAsync(interaction).ConfigureAwait(false);
        }
    }
}