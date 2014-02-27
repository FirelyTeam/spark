using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Service
{
    public interface IResourceImporter
    {
        void QueueNewResourceEntry(Uri id, Resource resource);
        void QueueNewResourceEntry(string collection, string id, Resource resource);
        void QueueNewDeletedEntry(string collection, string id);
        void QueueNewEntry(BundleEntry entry);
        IEnumerable<BundleEntry> ImportQueued();
    }
}
