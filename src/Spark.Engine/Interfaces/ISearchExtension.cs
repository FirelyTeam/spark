using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Service;
using Spark.Service;

namespace Spark.Engine.Interfaces
{
    public interface ISearchExtension : IFhirStoreExtension
    {
        Snapshot GetSnapshot(string type, SearchParams searchCommand);
        IKey FindSingle(string type, SearchParams searchCommand);
    }

    public class SearchExtension : ISearchExtension
    {
        private readonly IndexService indexService;
        private readonly IFhirIndex fhirIndex;
        private readonly ISnapshotStore snapshotStore;
        protected ILocalhost localhost;
        private IBaseFhirStore fhirStore;
        public const int MAX_PAGE_SIZE = 100;

        public SearchExtension(IndexService indexService, IFhirIndex fhirIndex, ILocalhost localhost, ISnapshotStore snapshotStore)
        {
            this.indexService = indexService;
            this.fhirIndex = fhirIndex;
            this.localhost = localhost;
            this.snapshotStore = snapshotStore;
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

        public void OnExtensionAdded(IBaseFhirStore fhirStore)
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
            snapshotStore.AddSnapshot(snapshot);
            return snapshot;
            //var snapshot = pager.CreateSnapshot(link, results, searchCommand);
            //Bundle bundle = pager.GetFirstPage(snapshot);

            //if (results.HasIssues)
            //{
            //    bundle.AddResourceEntry(results.Outcome, new Uri("outcome/1", UriKind.Relative).ToString());
            //}

            //return Respond.WithBundle(bundle);
        }

        public IKey FindSingle(string type, SearchParams searchCommand)
        {
            throw new NotImplementedException();
        }

        public Snapshot CreateSnapshot(Uri selflink, IEnumerable<string> keys, SearchParams searchCommand)
        {
            string sort = GetFirstSort(searchCommand);

            int? count = null;
            if (searchCommand.Count.HasValue)
            {
                count = Math.Min(searchCommand.Count.Value, MAX_PAGE_SIZE);
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


            return Snapshot.Create(Bundle.BundleType.Searchset, selflink, keys, sort, NormalizeCount(count), searchCommand.Include);
        }

        private int? NormalizeCount(int? count)
        {
            if (count.HasValue)
            {
                return Math.Min(count.Value, MAX_PAGE_SIZE);
            }
            return count;
        }
        private static string GetFirstSort(SearchParams searchCommand)
        {
            string firstSort = null;
            if (searchCommand.Sort != null && searchCommand.Sort.Count() > 0)
            {
                firstSort = searchCommand.Sort[0].Item1; //TODO: Support sortorder and multiple sort arguments.
            }
            return firstSort;
        }
    }
}