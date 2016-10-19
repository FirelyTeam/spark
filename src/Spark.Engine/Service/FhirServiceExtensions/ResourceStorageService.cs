using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class ResourceStorageService : IResourceStorageService
    {
        private readonly ITransfer transfer;
        private IFhirStore fhirStore;


        public ResourceStorageService(ITransfer transfer, IFhirStore fhirStore)
        {
            this.transfer = transfer;
            this.fhirStore = fhirStore;
        }

        public Entry Get(IKey key)
        {
            var entry = fhirStore.Get(key);
            if (entry != null)
            {
                transfer.Externalize(entry);
            }
            return entry;
        }

        public Entry Add(Entry entry)
        {
            if (entry.State != EntryState.Internal)
            {
                transfer.Internalize(entry);
            }
            fhirStore.Add(entry);
            Entry result;
            if (entry.IsDelete)
            {
                result = entry;
            }
            else
            {
                result = fhirStore.Get(entry.Key);
            }
            transfer.Externalize(result);

            return result;
        }

        public IList<Entry> Get(IEnumerable<string> localIdentifiers, string sortby = null)
        {
            IList<Entry> results = fhirStore.Get(localIdentifiers.Select(k => (IKey)Key.ParseOperationPath(k)));
            transfer.Externalize(results);
            return results;

        }
    }
}