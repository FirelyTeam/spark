using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    using System.Threading.Tasks;

    public class ResourceStorageService : IResourceStorageService
    {
        private readonly ITransfer transfer;
        private readonly IFhirStore fhirStore;


        public ResourceStorageService(ITransfer transfer, IFhirStore fhirStore)
        {
            this.transfer = transfer;
            this.fhirStore = fhirStore;
        }

        public async Task<Entry> Get(IKey key)
        {
            var entry = await fhirStore.Get(key).ConfigureAwait(false);
            if (entry != null)
            {
                transfer.Externalize(entry);
            }
            return entry;
        }

        public async Task<Entry> Add(Entry entry)
        {
            if (entry.State != EntryState.Internal)
            {
                transfer.Internalize(entry);
            }

            await fhirStore.Add(entry).ConfigureAwait(false);
            Entry result;
            if (entry.IsDelete)
            {
                result = entry;
            }
            else
            {
                result = await fhirStore.Get(entry.Key).ConfigureAwait(false);
            }
            transfer.Externalize(result);

            return result;
        }

        public async Task<IList<Entry>> Get(IEnumerable<string> localIdentifiers, string sortby = null)
        {
            IList<Entry> results = await fhirStore.Get(localIdentifiers.Select(k => (IKey)Key.ParseOperationPath(k))).ConfigureAwait(false);
            transfer.Externalize(results);
            return results;

        }
    }
}