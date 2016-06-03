using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class HistoryService : IHistoryService
    {
        private IHistoryStore historyStore;
        private readonly IFhirStore fhirStore;

        public HistoryService(IHistoryStore historyStore)
        {
            this.historyStore = historyStore;
        }

        public Snapshot History(string typename, HistoryParameters parameters)
        {
            return historyStore.History(typename, parameters);
        }

        public Snapshot History(IKey key, HistoryParameters parameters)
        {
            
            return historyStore.History(key, parameters);
        }

        public Snapshot History(HistoryParameters parameters)
        {
            return historyStore.History(parameters);
        }
      
    }
}