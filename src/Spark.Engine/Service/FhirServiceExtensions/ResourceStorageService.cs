using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        [Obsolete("Use Async method version instead")]
        public Entry Get(IKey key)
        {
            return Task.Run(() => GetAsync(key)).GetAwaiter().GetResult();
        }

        [Obsolete("Use Async method version instead")]
        public Entry Add(Entry entry)
        {
            return Task.Run(() => AddAsync(entry)).GetAwaiter().GetResult();
        }

        [Obsolete("Use Async method version instead")]
        public IList<Entry> Get(IEnumerable<string> localIdentifiers, string sortby = null)
        {
            return Task.Run(() => GetAsync(localIdentifiers, sortby)).GetAwaiter().GetResult();
        }

        public async Task<Entry> GetAsync(IKey key)
        {
            var entry = await fhirStore.GetAsync(key).ConfigureAwait(false);
            if (entry != null)
            {
                transfer.Externalize(entry);
            }
            return entry;
        }

        public async Task<Entry> AddAsync(Entry entry)
        {
            if (entry.State != EntryState.Internal)
            {
                transfer.Internalize(entry);
            }
            await fhirStore.AddAsync(entry).ConfigureAwait(false);
            Entry result;
            if (entry.IsDelete)
            {
                result = entry;
            }
            else
            {
                result = await fhirStore.GetAsync(entry.Key).ConfigureAwait(false);
            }
            transfer.Externalize(result);

            return result;
        }

        public async Task<IList<Entry>> GetAsync(IEnumerable<string> localIdentifiers, string sortby = null)
        {
            IList<Entry> results = await fhirStore.GetAsync(localIdentifiers.Select(k => (IKey)Key.ParseOperationPath(k))).ConfigureAwait(false);
            transfer.Externalize(results);
            return results;

        }
    }
}