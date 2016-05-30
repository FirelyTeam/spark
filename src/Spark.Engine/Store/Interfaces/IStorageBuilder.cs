using Spark.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface IStorageBuilder
    {
        IFhirStore GetStore();
        IHistoryStore GetHistoryStore();
        IIndexStore GetIndexStore();
        IFhirIndex GetFhirIndex();
        ISnapshotStore GeSnapshotStore();
    }
}