using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface ISnapshotStore
    {
        Task AddSnapshot(Snapshot snapshot);
        Task<Snapshot> GetSnapshot(string snapshotid);
    }
}
