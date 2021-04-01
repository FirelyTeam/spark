using System;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface IHistoryStore
    {
        [Obsolete("Use HistoryAsync(string, HistoryParameters) instead")]
        Snapshot History(string typename, HistoryParameters parameters);

        [Obsolete("Use HistoryAsync(IKey, HistoryParameters) instead")]
        Snapshot History(IKey key, HistoryParameters parameters);

        [Obsolete("Use HistoryAsync(HistoryParameters) instead")]
        Snapshot History(HistoryParameters parameters);

        Task<Snapshot> HistoryAsync(string typename, HistoryParameters parameters);

        Task<Snapshot> HistoryAsync(IKey key, HistoryParameters parameters);

        Task<Snapshot> HistoryAsync(HistoryParameters parameters);
    }
}