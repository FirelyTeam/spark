using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Store.Interfaces;
using Spark.Service;

namespace Spark.Engine.Storage.StoreExtensions
{
    public class PagingExtension : IPagingExtension
    {
        private readonly ISnapshotStore snapshotstore;
        private readonly ITransfer transfer;
        private IFhirStore fhirStore;

        public PagingExtension(ISnapshotStore snapshotstore, ITransfer transfer)
        {
            this.snapshotstore = snapshotstore;
            this.transfer = transfer;
        }

        public void OnExtensionAdded(IFhirStore extensibleObject)
        {
            fhirStore = extensibleObject;
        }

        public void OnEntryAdded(Entry entry)
        {
        }


        public SnapshotPagination CreatePagination(Snapshot snapshot)
        {
            snapshotstore.AddSnapshot(snapshot);
            return new SnapshotPagination(snapshot, fhirStore, transfer);
        }

        public SnapshotPagination CreatePagination(string snapshotkey)
        {
            Snapshot snapshot = snapshotstore.GetSnapshot(snapshotkey);
            return new SnapshotPagination(snapshot, fhirStore, transfer);
        }
     
    }
}