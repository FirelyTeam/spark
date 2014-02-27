using Hl7.Fhir.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spark.Core
{
    public interface IFhirStore
    {
        BundleEntry FindEntryById(Uri url);
        BundleEntry AddEntry(BundleEntry entry, Guid? transactionId = null);
        BundleEntry FindVersionByVersionId(Uri url);
        int GenerateNewIdSequenceNumber();

        IEnumerable<BundleEntry> ListCollection(string collectionName, bool includeDeleted = false, DateTimeOffset? since = null, int limit = 100);
        IEnumerable<BundleEntry> ListVersionsInCollection(string collectionName, DateTimeOffset? since = null, int limit = 100);
        IEnumerable<BundleEntry> ListVersionsById(Uri url, DateTimeOffset? since = null, int limit = 100);
        IEnumerable<BundleEntry> ListVersions(DateTimeOffset? since = null, int limit = 100);
        void PurgeBatch(Guid batchId);
        void StoreSnapshot(Snapshot snap);
        Snapshot GetSnapshot(string snapshotId);
        IEnumerable<BundleEntry> FindByVersionIds(IEnumerable<Uri> entryVersionIds);
        IEnumerable<BundleEntry> AddEntries(IEnumerable<BundleEntry> entries, Guid? batchId = null);
        void ReplaceEntry(ResourceEntry entry, Guid? batchId = null);

        IEnumerable<Tag> ListTagsInServer();
        IEnumerable<Tag> ListTagsInCollection(string collection);
        
        void Clean();

        void EnsureNextSequenceNumberHigherThan(int seq);
        int GenerateNewVersionSequenceNumber();
    }
}