using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Storage.StoreExtensions;

namespace Spark.Engine.Store.Interfaces
{
    public interface IPagingExtension : IFhirStoreExtension
    {
        SnapshotPagination CreatePagination(Snapshot snapshot);
        SnapshotPagination CreatePagination(string snapshotkey);
    }
}