/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Spark.Data;
using Spark.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Spark.Support;
using Hl7.Fhir.Rest;
using Spark.Config;
using Spark.Core;

namespace Spark.Service
{

    internal class Pager
    {
        IFhirStore store;
        ResourceExporter exporter;
        public const int MAX_PAGE_SIZE = 100;
        public const int DEFAULT_PAGE_SIZE = 20;

        public Pager(IFhirStore store, ResourceExporter exporter)
        {
            this.store = store;
            this.exporter = exporter;
        }

        public Bundle GetPage(string snapshotkey, int start = 0, int count = DEFAULT_PAGE_SIZE)
        {
            Snapshot snapshot = store.GetSnapshot(snapshotkey);
            return GetPage(snapshot, start, count);
        }

        public Bundle GetPage(Snapshot snapshot, int start = 0, int pagesize = DEFAULT_PAGE_SIZE)
        {
            if (pagesize > MAX_PAGE_SIZE) pagesize = MAX_PAGE_SIZE;

            if (snapshot == null)
                throw new SparkException(HttpStatusCode.NotFound, "There is no paged snapshot with id '{0}'", snapshot.Id);

            if (!snapshot.InRange(start))
            {
                throw new SparkException(HttpStatusCode.NotFound, 
                    "The specified index lies outside the range of available results ({0}) in snapshot {1}",
                    snapshot.Keys.Count(), snapshot.Id);
            }

            
            return CreateBundle(snapshot, start, pagesize);
        }

        public Bundle CreateSnapshotAndGetFirstPage(string title, Uri link, IEnumerable<Uri> keys, string sortby, IEnumerable<string> includes = null)
        {
            Snapshot snapshot = Snapshot.Create(title, link, keys, sortby, includes);
            store.AddSnapshot(snapshot);
            Bundle bundle = this.GetPage(snapshot);
            return bundle;
        }

        public Bundle CreateBundle(Snapshot snapshot, int start, int count)
        {
            Bundle bundle = new Bundle();
            bundle.Title = snapshot.FeedTitle;
            bundle.TotalResults = snapshot.Count;
            bundle.Id = Key.NewUuid();
            bundle.AuthorName = "Furore Spark FHIR server";
            bundle.AuthorUri = "http://fhir.furore.com";
            
            bundle.Links = new UriLinkList();
            bundle.Links.SelfLink = new Uri(snapshot.FeedSelfLink);
            bundle.LastUpdated = snapshot.WhenCreated;

            IEnumerable<Uri> keys = snapshot.Keys.Skip(start).Take(count);
            bundle.Entries = store.Get(keys, snapshot.SortBy).ToList();
            Include(bundle, snapshot.Includes);
            buildLinks(bundle, snapshot, start, count);
            exporter.Externalize(bundle);
            return bundle;
        }

        // Given a set of version id's, go fetch a subset of them from the store and build a Bundle
        /*private Bundle createBundle(Snapshot snapshot, int start, int count)
        {
            var entryVersionIds = snapshot.Keys.Skip(start).Take(count).ToList();
            var pageContents = store.Get(entryVersionIds, snapshot.SortBy).ToList();

            var bundle =
                BundleEntryFactory.CreateBundleWithEntries(snapshot.FeedTitle, new Uri(snapshot.FeedSelfLink),
                      "Spark MatchBox Search Engine", null, pageContents);

            if (snapshot.Count != Snapshot.NOCOUNT)
                bundle.TotalResults = snapshot.Count;
            else
                bundle.TotalResults = null;

            var total = snapshot.Keys.Count();

            // If we need paging, add the paging links
            if (total > count)
                buildLinks(bundle, snapshot, start, count);

            return bundle;
        }
        */

        private static void buildLinks(Bundle bundle, Snapshot snapshot, int start, int count)
        {
            var lastPage = snapshot.Count / count;

            // http://spark.furore.com/fhir/_snapshot/

            Uri baseurl = new Uri(Settings.Endpoint.ToString() + "/" + FhirRestOp.SNAPSHOT);

            bundle.Links.SelfLink =
                baseurl
                .AddParam(FhirParameter.SNAPSHOT_ID, snapshot.Id)
                .AddParam(FhirParameter.SNAPSHOT_INDEX, start.ToString())
                .AddParam(FhirParameter.COUNT, count.ToString());

            // First
            bundle.Links.FirstLink =
                baseurl
                .AddParam(FhirParameter.SNAPSHOT_ID, snapshot.Id)
                .AddParam(FhirParameter.SNAPSHOT_INDEX, "0");

            // Last
            bundle.Links.LastLink =
                baseurl
                .AddParam(FhirParameter.SNAPSHOT_ID, snapshot.Id)
                .AddParam(FhirParameter.SNAPSHOT_INDEX, (lastPage * count).ToString());

            // Only do a Previous if we can go back
            if (start > 0)
            {
                int prevIndex = start - count;
                if (prevIndex < 0) prevIndex = 0;

                bundle.Links.PreviousLink =
                    baseurl
                    .AddParam(FhirParameter.SNAPSHOT_ID, snapshot.Id)
                    .AddParam(FhirParameter.SNAPSHOT_INDEX, prevIndex.ToString());
            }

            // Only do a Next if we can go forward
            if (start + count < snapshot.Count)
            {
                int nextIndex = start + count;

                bundle.Links.NextLink =
                    baseurl
                    .AddParam(FhirParameter.SNAPSHOT_ID, snapshot.Id)
                    .AddParam(FhirParameter.SNAPSHOT_INDEX, nextIndex.ToString());
            }
        }
       

        public void Include(Bundle bundle, IEnumerable<string> includes)
        {
            if (includes == null) return;

            IEnumerable<Uri> keys = bundle.GetReferences(includes).Distinct();
            IEnumerable<BundleEntry> entries = store.Get(keys, null);
            bundle.AddRange(entries);
        }

    }


    
    
}
