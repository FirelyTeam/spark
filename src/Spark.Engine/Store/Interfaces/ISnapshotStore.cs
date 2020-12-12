using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    using System.Threading.Tasks;

    public interface ISnapshotStore
    {
        Task AddSnapshot(Snapshot snapshot);
        Task<Snapshot> GetSnapshot(string snapshotid);
    }
}
