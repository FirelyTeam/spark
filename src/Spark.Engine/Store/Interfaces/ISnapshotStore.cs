using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface ISnapshotStore
    {
        void AddSnapshot(Snapshot snapshot);
        Snapshot GetSnapshot(string snapshotid);
    }
}
