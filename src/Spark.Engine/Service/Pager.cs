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

namespace Spark.Service
{

    public class Pager
    {
        IFhirStore store;
        ISnapshotStore snapshotstore;
        ILocalhost localhost;
        Transfer transfer;

        public const int MAX_PAGE_SIZE = 100;
        public const int DEFAULT_PAGE_SIZE = 20;

        public Pager(IFhirStore store, ISnapshotStore snapshotstore, ILocalhost localhost, Transfer transfer)
        {
            this.store = store;
            this.snapshotstore = snapshotstore;
            this.localhost = localhost;
            this.transfer = transfer;
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

        public Bundle GetFirstPage(Snapshot snapshot)
        {
            Bundle bundle = this.GetPage(snapshot, 0);
            return bundle;
        }

        public Snapshot CreateSnapshot(Bundle.BundleType type, Uri link, IEnumerable<string> keys, string sortby, IEnumerable<string> includes = null)
        {
            Snapshot snapshot = Snapshot.Create(type, link, keys, sortby, includes);
            snapshotstore.AddSnapshot(snapshot);
            return snapshot;
        }

        public Bundle CreateBundle(Snapshot snapshot, int start, int count)
        {
            Bundle bundle = new Bundle();
            bundle.Type = snapshot.Type;
            bundle.Total = snapshot.Count;
            bundle.Id = UriHelper.CreateUuid().ToString();

            IList<string> keys = snapshot.Keys.Skip(start).Take(count).ToList();
            IList<Interaction> interactions = store.Get(keys, snapshot.SortBy).ToList();
            transfer.Externalize(interactions);

            bundle.Append(interactions);

            Include(bundle, snapshot.Includes);
            BuildLinks(bundle, snapshot, start, count);
            
            
            return bundle;
        }

        void BuildLinks(Bundle bundle, Snapshot snapshot, int start, int count)
        {
            var lastPage = snapshot.Count / count;
            Uri baseurl = new Uri(localhost.Base.ToString() + "/" + FhirRestOp.SNAPSHOT);

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
                .AddParam(FhirParameter.SNAPSHOT_INDEX, (lastPage * count).ToString());

            // Only do a Previous if we can go back
            if (start > 0)
            {
                int prevIndex = start - count;
                if (prevIndex < 0) prevIndex = 0;

                bundle.PreviousLink =
                    baseurl
                    .AddParam(FhirParameter.SNAPSHOT_ID, snapshot.Id)
                    .AddParam(FhirParameter.SNAPSHOT_INDEX, prevIndex.ToString());
            }

            // Only do a Next if we can go forward
            if (start + count < snapshot.Count)
            {
                int nextIndex = start + count;

                bundle.NextLink =
                    baseurl
                    .AddParam(FhirParameter.SNAPSHOT_ID, snapshot.Id)
                    .AddParam(FhirParameter.SNAPSHOT_INDEX, nextIndex.ToString());
            }

        }

        private void Include(Bundle bundle, IEnumerable<string> includes)
        {
            if (includes == null) return;

            // DSTU2: paging
            /*
            IEnumerable<Uri> keys = bundle.GetReferences(includes).Distinct();
            IEnumerable<BundleEntry> entries = store.Get(keys, null);
            bundle.AddRange(entries);
            */
        }

    }


    
    
}
