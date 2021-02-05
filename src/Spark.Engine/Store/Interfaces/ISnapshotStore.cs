using System;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface ISnapshotStore
    {
        [Obsolete("Use Async method version instead")]
        void AddSnapshot(Snapshot snapshot);

        [Obsolete("Use Async method version instead")]
        Snapshot GetSnapshot(string snapshotid);

        Task AddSnapshotAsync(Snapshot snapshot);

        Task<Snapshot> GetSnapshotAsync(string snapshotId);
    }
}
