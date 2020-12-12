using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    using System.Threading.Tasks;

    public interface IHistoryStore
    {
        Task<Snapshot> History(string typename, HistoryParameters parameters);
        Task<Snapshot> History(IKey key, HistoryParameters parameters);
        Task<Snapshot> History(HistoryParameters parameters);
    }
}