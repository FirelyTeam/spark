using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Service;

namespace Spark.Engine.Interfaces
{
    public interface IPagingExtension : IFhirStoreExtension
    {
        Bundle GetPage(string snapshotkey, int index);
    }

    public class PagingExtension : IPagingExtension
    {
        public const int DEFAULT_PAGE_SIZE = 20;

        private readonly ISnapshotStore snapshotstore;
        private IBaseFhirStore fhirStore;
        private readonly ITransfer transfer;
        private readonly ILocalhost localhost;

        public PagingExtension(ISnapshotStore snapshotstore, ITransfer transfer, ILocalhost localhost)
        {
            this.snapshotstore = snapshotstore;
            this.transfer = transfer;
            this.localhost = localhost;
        }

        public void OnExtensionAdded(IBaseFhirStore extensibleObject)
        {
            fhirStore = extensibleObject;
        }

        public void OnEntryAdded(Entry entry)
        {
        }

        public Bundle GetPage(string snapshotkey, int index)
        {
            Snapshot snapshot = snapshotstore.GetSnapshot(snapshotkey);
            return GetPage(snapshot, index);
        }

        public Bundle GetPage(Snapshot snapshot, int? start = null)
        {
            //if (pagesize > MAX_PAGE_SIZE) pagesize = MAX_PAGE_SIZE;

            if (snapshot == null)
                throw Error.NotFound("There is no paged snapshot with id '{0}'", snapshot.Id);

            if (start.HasValue && !snapshot.InRange(start.Value))
            {
                throw Error.NotFound(
                    "The specified index lies outside the range of available results ({0}) in snapshot {1}",
                    snapshot.Keys.Count(), snapshot.Id);
            }

            return this.CreateBundle(snapshot, start);
        }

        public Bundle CreateBundle(Snapshot snapshot, int? start = null)
        {
            Bundle bundle = new Bundle();
            bundle.Type = snapshot.Type;
            bundle.Total = snapshot.Count;
            bundle.Id = UriHelper.CreateUuid().ToString();

            IEnumerable<string> keysInBundle = snapshot.Keys;
            if (start.HasValue)
            {
                keysInBundle = keysInBundle.Skip(start.Value);
            }

            IList<string> keys = keysInBundle.Take(snapshot.CountParam ?? DEFAULT_PAGE_SIZE).ToList();
            IList<Entry> entry = fhirStore.Get(keys, snapshot.SortBy).ToList();

            IList<Entry> included = GetIncludesRecursiveFor(entry, snapshot.Includes);
            entry.Append(included);

            transfer.Externalize(entry);
            bundle.Append(entry);
            BuildLinks(bundle, snapshot, start);

            return bundle;
        }


        void BuildLinks(Bundle bundle, Snapshot snapshot, int? start = null)
        {
            int countParam = snapshot.CountParam ?? DEFAULT_PAGE_SIZE;

            Uri baseurl = new Uri(localhost.DefaultBase.ToString() + "/" + FhirRestOp.SNAPSHOT);

            if (start.HasValue)
            {
                bundle.SelfLink = BuildSnapshotPageLink(baseurl, snapshot.Id, start.Value);
            }
            else
            {
                bundle.SelfLink = new Uri(snapshot.FeedSelfLink);

            }

            // First
            bundle.FirstLink = BuildSnapshotPageLink(baseurl, snapshot.Id, 0);

            // Last
            if (snapshot.Count > countParam)
            {
                int numberOfPages = snapshot.Count / countParam;
                int lastPageIndex = (snapshot.Count % countParam == 0) ? numberOfPages - 1 : numberOfPages;
                bundle.LastLink = BuildSnapshotPageLink(baseurl, snapshot.Id, (lastPageIndex * countParam));
            }
            else
            {
                bundle.LastLink = BuildSnapshotPageLink(baseurl, snapshot.Id, 0);
            }

            // Only do a Previous if we can go back
            if (start.HasValue && start.Value > 0)
            {
                bundle.PreviousLink = BuildSnapshotPageLink(baseurl, snapshot.Id, start.Value - countParam);
            }

            // Only do a Next if we can go forward
            if (((start ?? 0) + countParam) < snapshot.Count)
            {
                bundle.NextLink = BuildSnapshotPageLink(baseurl, snapshot.Id, (start ?? 0) + countParam);
            }
        }

        private Uri BuildSnapshotPageLink(Uri baseurl, string snapshotId, int snapshotIndex)
        {
            return baseurl
                        .AddParam(FhirParameter.SNAPSHOT_ID, snapshotId)
                        .AddParam(FhirParameter.SNAPSHOT_INDEX, snapshotIndex.ToString());
        }
        private IList<Entry> GetIncludesRecursiveFor(IList<Entry> entries, IEnumerable<string> includes)
        {
            IList<Entry> included = new List<Entry>();

            var latest = GetIncludesFor(entries, includes);
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

        private IList<Entry> GetIncludesFor(IList<Entry> entries, IEnumerable<string> includes)
        {
            if (includes == null) return new List<Entry>();

            IEnumerable<string> paths = includes.SelectMany(i => IncludeToPath(i));
            IList<string> identifiers = entries.GetResources().GetReferences(paths).Distinct().ToList();

            IList<Entry> result = fhirStore.GetCurrent(identifiers, null).ToList();

            return result;
        }

        private IEnumerable<string> IncludeToPath(string include)
        {
            string[] _include = include.Split(':');
            string resource = _include.FirstOrDefault();
            string paramname = _include.Skip(1).FirstOrDefault();
            var param = ModelInfo.SearchParameters.FirstOrDefault(p => p.Resource == resource && p.Name == paramname);
            if (param != null)
            {
                return param.Path;
            }
            else
            {
                return Enumerable.Empty<string>();
            }
        }
    }
}