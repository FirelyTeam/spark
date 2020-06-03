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

        public Task<Snapshot> History(string typename, HistoryParameters parameters)
        {
            return historyStore.History(typename, parameters);
        }

        public Task<Snapshot> History(IKey key, HistoryParameters parameters)
        {
            
            return historyStore.History(key, parameters);
        }

        public Task<Snapshot> History(HistoryParameters parameters)
        {
            return historyStore.History(parameters);
        }
      
    }
}
