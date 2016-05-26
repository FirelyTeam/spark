using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Interfaces;
using Spark.Engine.Store.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service.Extensions
{
    public class SearchExtension : ISearchExtension
    {
        private readonly IndexService indexService;
        private readonly IFhirIndex fhirIndex;
        protected ILocalhost localhost;
        private IFhirStore fhirStore;

        public SearchExtension(IndexService indexService, IFhirIndex fhirIndex, ILocalhost localhost)
        {
            this.indexService = indexService;
            this.fhirIndex = fhirIndex;
            this.localhost = localhost;
        }

        public void OnEntryAdded(Entry entry)
        {
            if (indexService != null)
            {
                indexService.Process(entry);
            }

            else if (fhirIndex != null)
            {
                //TODO: If IndexService is working correctly, remove the reference to fhirIndex.
                fhirIndex.Process(entry);
            }
        }

        public void OnExtensionAdded(IFhirStore fhirStore)
        {
            this.fhirStore = fhirStore;
        }

        public Snapshot GetSnapshot(string type, SearchParams searchCommand)
        {
            Validate.TypeName(type);
            SearchResults results = fhirIndex.Search(type, searchCommand);

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

        public IKey FindSingle(string type, SearchParams searchCommand)
        {
            throw new NotImplementedException();
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

            if (string.IsNullOrEmpty(sort) == false)
            {
                selflink = selflink.AddParam(SearchParams.SEARCH_PARAM_SORT, new string[] { sort });
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
    }
}