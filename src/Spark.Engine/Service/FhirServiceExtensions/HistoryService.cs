using System;
using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class HistoryService : IHistoryService
    {
        private IHistoryStore historyStore;

        public HistoryService(IHistoryStore historyStore)
        {
            this.historyStore = historyStore;
        }

        [Obsolete("Use Async method version instead")]
        public Snapshot History(string typename, HistoryParameters parameters)
        {
            return Task.Run(() => HistoryAsync(typename, parameters)).GetAwaiter().GetResult();
        }

        [Obsolete("Use Async method version instead")]
        public Snapshot History(IKey key, HistoryParameters parameters)
        {
            return Task.Run(() => HistoryAsync(key, parameters)).GetAwaiter().GetResult();
        }

        [Obsolete("Use Async method version instead")]
        public Snapshot History(HistoryParameters parameters)
        {
            return Task.Run(() => HistoryAsync(parameters)).GetAwaiter().GetResult();
        }

        public async Task<Snapshot> HistoryAsync(string typename, HistoryParameters parameters)
        {
            return await historyStore.HistoryAsync(typename, parameters).ConfigureAwait(false);
        }

        public async Task<Snapshot> HistoryAsync(IKey key, HistoryParameters parameters)
        {

            return await historyStore.HistoryAsync(key, parameters).ConfigureAwait(false);
        }

        public async Task<Snapshot> HistoryAsync(HistoryParameters parameters)
        {
            return await historyStore.HistoryAsync(parameters).ConfigureAwait(false);
        }

    }
}