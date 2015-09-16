/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Hl7.Fhir.Rest;

namespace Spark.Service
{

    public class Pager
    {
        IFhirStore fhirStore;
        ISnapshotStore snapshotstore;
        ILocalhost localhost;
        Transfer transfer;
        IList<ModelInfo.SearchParamDefinition> searchParameters;

        public const int MAX_PAGE_SIZE = 100;
        public const int DEFAULT_PAGE_SIZE = 20;

        public Pager(IFhirStore fhirStore, ISnapshotStore snapshotstore, ILocalhost localhost, Transfer transfer, List<ModelInfo.SearchParamDefinition> searchParameters)
        {
            this.fhirStore = fhirStore;
            this.snapshotstore = snapshotstore;
            this.localhost = localhost;
            this.transfer = transfer;
            this.searchParameters = searchParameters;
        }

        public Bundle GetPage(string snapshotkey, int start = 0, int count = DEFAULT_PAGE_SIZE)
        {
            Snapshot snapshot = snapshotstore.GetSnapshot(snapshotkey);
            return GetPage(snapshot, start, count);
        }

        public Bundle GetPage(Snapshot snapshot, int start, int pagesize = DEFAULT_PAGE_SIZE)
        {
            if (pagesize > MAX_PAGE_SIZE) pagesize = MAX_PAGE_SIZE;

            if (snapshot == null)
                throw Error.NotFound("There is no paged snapshot with id '{0}'", snapshot.Id);

            if (!snapshot.InRange(start))
            {
                throw Error.NotFound(
                    "The specified index lies outside the range of available results ({0}) in snapshot {1}",
                    snapshot.Keys.Count(), snapshot.Id);
            }

            return this.CreateBundle(snapshot, start, pagesize);
        }

        public Bundle GetFirstPage(Snapshot snapshot, int? pagesize = null)
        {
            Bundle bundle = this.GetPage(snapshot, 0, pagesize?? DEFAULT_PAGE_SIZE);
            return bundle;
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


        /// <summary>
        /// Creates a snapshot for search commands
        /// </summary>
        public Snapshot CreateSnapshot(Bundle.BundleType type, Uri link, IEnumerable<string> keys, string sortby = null, IList<string> includes = null)
        {
            
            Snapshot snapshot = Snapshot.Create(type, link, keys, sortby, includes);
            snapshotstore.AddSnapshot(snapshot);
            return snapshot;
        }

        public Snapshot CreateSnapshot(Uri selflink, IEnumerable<string> keys, SearchParams searchCommand)
        {
            string sort = GetFirstSort(searchCommand);
            return CreateSnapshot(Bundle.BundleType.Searchset, selflink, keys, sort, searchCommand.Include);
        }

        public Bundle CreateBundle(Snapshot snapshot, int start, int count)
        {
            Bundle bundle = new Bundle();
            bundle.Type = snapshot.Type;
            bundle.Total = snapshot.Count;
            bundle.Id = UriHelper.CreateUuid().ToString();

            IList<string> keys = snapshot.Keys.Skip(start).Take(count).ToList();
            IList<Interaction> interactions = fhirStore.Get(keys, snapshot.SortBy).ToList();

            IList<Interaction> included = GetIncludesRecursiveFor(interactions, snapshot.Includes);
            interactions.Append(included);

            transfer.Externalize(interactions);
            bundle.Append(interactions);
            BuildLinks(bundle, snapshot, start, count);

            return bundle;
        }

        void BuildLinks(Bundle bundle, Snapshot snapshot, int start, int count)
        {
            int numberOfPages = snapshot.Count/count;
            var lastPageIndex = (snapshot.Count % count == 0) ? numberOfPages - 1 : numberOfPages ;
            Uri baseurl = new Uri(localhost.DefaultBase.ToString() + "/" + FhirRestOp.SNAPSHOT);

            bundle.SelfLink =
                baseurl
                .AddParam(FhirParameter.SNAPSHOT_ID, snapshot.Id)
                .AddParam(FhirParameter.SNAPSHOT_INDEX, start.ToString())
                .AddParam(FhirParameter.COUNT, count.ToString());

            // First
            bundle.FirstLink =
                baseurl
                .AddParam(FhirParameter.SNAPSHOT_ID, snapshot.Id)
                .AddParam(FhirParameter.SNAPSHOT_INDEX, "0");

            // Last
            bundle.LastLink =
                baseurl
                .AddParam(FhirParameter.SNAPSHOT_ID, snapshot.Id)
                .AddParam(FhirParameter.SNAPSHOT_INDEX, (lastPageIndex * count).ToString())
                .AddParam(FhirParameter.COUNT, count.ToString());

            // Only do a Previous if we can go back
            if (start > 0)
            {
                int prevIndex = start - count;
                if (prevIndex < 0) prevIndex = 0;

                bundle.PreviousLink =
                    baseurl
                    .AddParam(FhirParameter.SNAPSHOT_ID, snapshot.Id)
                    .AddParam(FhirParameter.SNAPSHOT_INDEX, prevIndex.ToString())
                    .AddParam(FhirParameter.COUNT, count.ToString());
            }

            // Only do a Next if we can go forward
            if (start + count < snapshot.Count)
            {
                int nextIndex = start + count;

                bundle.NextLink =
                    baseurl
                        .AddParam(FhirParameter.SNAPSHOT_ID, snapshot.Id)
                        .AddParam(FhirParameter.SNAPSHOT_INDEX, nextIndex.ToString())
                        .AddParam(FhirParameter.COUNT, count.ToString());
            }

        }

        private IEnumerable<string> IncludeToPath(string include)
        {
            string[] _include = include.Split(':');
            string resource = _include.FirstOrDefault();
            string paramname = _include.Skip(1).FirstOrDefault();
            var param = searchParameters.FirstOrDefault(p => p.Resource == resource && p.Name == paramname);
            if (param != null)
            {
                return param.Path;
            }
            else
            {
                return Enumerable.Empty<string>();
            }
        }

        private IList<Interaction> GetIncludesFor(IList<Interaction> interactions, IEnumerable<string> includes)
        {
            if (includes == null) return new List<Interaction>();

            IEnumerable<string> paths = includes.SelectMany(i => IncludeToPath(i)); 
            IList<string> identifiers = interactions.GetResources().GetReferences(paths).Distinct().ToList();

            IList<Interaction> entries = fhirStore.GetCurrent(identifiers, null).ToList();

            return entries;
        }

        private IList<Interaction> GetIncludesRecursiveFor(IList<Interaction> interactions, IEnumerable<string> includes)
        {
            IList<Interaction> included = new List<Interaction>();

            var latest = GetIncludesFor(interactions, includes);
            int previouscount;
            do
            {
                previouscount = included.Count;
                included.AppendDistinct(latest);
                latest = GetIncludesFor(latest, includes);
            }
            while (included.Count > previouscount);
            return included;
        }

    }


    
    
}
