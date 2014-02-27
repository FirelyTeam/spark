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
        
        IFhirStore _store;
        public const int MAX_PAGE_SIZE = 100;

        public Pager(IFhirStore store)
        {
            this._store = store;
        }

        public Bundle FirstPage(Bundle bundle, int count)
        {
            Snapshot snapshot = Snapshot.TakeSnapshotFromBundle(bundle);
            
            if (bundle.Entries.Count > count)
            {
                _store.StoreSnapshot(snapshot);
            }
            
            // Return the first page
            return GetPage(snapshot, 0, count);
        }

        public Bundle FirstPage(Snapshot snapshot, int count)
        {
            _store.StoreSnapshot(snapshot);

            // Return the first page
            return GetPage(snapshot.Id, 0, count);
        }

        public Bundle GetPage(string snapshotId, int start, int count)
        {
            Snapshot snapshot = _store.GetSnapshot(snapshotId);
            return GetPage(snapshot, start, count);
        }
        public Bundle GetPage(Snapshot snapshot, int start, int pagesize)
        {
            if (pagesize > MAX_PAGE_SIZE) pagesize = MAX_PAGE_SIZE;

            if (snapshot == null)
                throw new SparkException(HttpStatusCode.NotFound, "There is no paged snapshot with id '{0}'", snapshot.Id);

            if (!snapshot.InRange(start))
            {
                throw new SparkException(HttpStatusCode.NotFound, 
                    "The specified index lies outside the range of available results ({0}) in snapshot {1}",
                    snapshot.Contents.Count(), snapshot.Id);
            }

            return createBundle(snapshot, start, pagesize);
        }

        // Given a set of version id's, go fetch a subset of them from the store and build a Bundle
        private Bundle createBundle(Snapshot snapshot, int start, int count)
        {
            var entryVersionIds = snapshot.Contents.Skip(start).Take(count).ToList();
            var pageContents = _store.FindByVersionIds(entryVersionIds).ToList();

            var resultBundle =
                BundleEntryFactory.CreateBundleWithEntries(snapshot.FeedTitle, new Uri(snapshot.FeedSelfLink),
                      "Spark MatchBox Search Engine", null, pageContents);

            if (snapshot.MatchCount != Snapshot.NOCOUNT)
                resultBundle.TotalResults = snapshot.MatchCount;
            else
                resultBundle.TotalResults = null;

            // If we need paging, add the paging links
            if (snapshot.Contents.Count() > count)
                buildLinks(resultBundle, snapshot.Id, start, count, resultBundle.TotalResults.Value);

            return resultBundle;
        }


        
        private static void buildLinks(Bundle bundle, string snapshotId, int index, int pageSize, int count)
        {
            var lastPage = count / pageSize;
                        
            // http://spark.furore.com/fhir/_snapshot/

            Uri baseurl = new Uri(Settings.Endpoint.ToString() + "/" + FhirRestOp.SNAPSHOT);

            bundle.Links.SelfLink = 
                baseurl
                .AddParam(FhirParameter.SNAPSHOT_ID, snapshotId)
                .AddParam(FhirParameter.SNAPSHOT_INDEX, index.ToString())
                .AddParam(FhirParameter.COUNT, pageSize.ToString());

            // First
            bundle.Links.FirstLink = 
                baseurl
                .AddParam(FhirParameter.SNAPSHOT_ID, snapshotId)
                .AddParam(FhirParameter.SNAPSHOT_INDEX, "0");

            // Last
            bundle.Links.LastLink = 
                baseurl
                .AddParam(FhirParameter.SNAPSHOT_ID, snapshotId)
                .AddParam(FhirParameter.SNAPSHOT_INDEX, (lastPage * pageSize).ToString());

            // Only do a Previous if we can go back
            if (index > 0)
            {
                int prevIndex = index - pageSize;
                if (prevIndex < 0) prevIndex = 0;
                
                bundle.Links.PreviousLink = 
                    baseurl
                    .AddParam(FhirParameter.SNAPSHOT_ID, snapshotId)
                    .AddParam(FhirParameter.SNAPSHOT_INDEX, prevIndex.ToString());
            }

            // Only do a Next if we can go forward
            if (index + pageSize < count)
            {
                int nextIndex = index + pageSize;
                
                bundle.Links.NextLink = 
                    baseurl
                    .AddParam(FhirParameter.SNAPSHOT_ID, snapshotId)
                    .AddParam(FhirParameter.SNAPSHOT_INDEX, nextIndex.ToString());
            }
        }
    }

    
}
