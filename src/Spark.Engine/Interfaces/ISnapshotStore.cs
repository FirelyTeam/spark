using Spark.Engine.Core;

namespace Spark.Core
{
    public interface ISnapshotStore
    {
        void AddSnapshot(Snapshot snapshot);
        Snapshot GetSnapshot(string snapshotid);
    }
}
