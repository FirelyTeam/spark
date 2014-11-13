using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public interface IFhirStorage
    {
        // Keys
        IEnumerable<Uri> List(string resource, DateTimeOffset? since = null);
        IEnumerable<Uri> History(string resource, DateTimeOffset? since = null);
        IEnumerable<Uri> History(Uri key, DateTimeOffset? since = null);
        IEnumerable<Uri> History(DateTimeOffset? since = null);

        // BundleEntries
        bool Exists(Uri key);

        BundleEntry Get(Uri key);
        IEnumerable<BundleEntry> Get(IEnumerable<Uri> keys, string sortby);

        void Add(BundleEntry entry);
        void Add(IEnumerable<BundleEntry> entries);

        void Replace(BundleEntry entry);

        // Snapshots
        void AddSnapshot(Snapshot snapshot);
        Snapshot GetSnapshot(string key);

        void Clean();
    }
}
