using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface IHistoryStore
    {
        Task<Snapshot> History(string typename, HistoryParameters parameters);
        Task<Snapshot> History(IKey key, HistoryParameters parameters);
        Task<Snapshot> History(HistoryParameters parameters);
    }
}
