/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

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
        private readonly ITransfer _transfer;
        private readonly IFhirStore _fhirStore;


        public ResourceStorageService(ITransfer transfer, IFhirStore fhirStore)
        {
            _transfer = transfer;
            _fhirStore = fhirStore;
        }

        public Entry Get(IKey key)
        {
            var entry = _fhirStore.Get(key);
            if (entry != null)
            {
                _transfer.Externalize(entry);
            }
            return entry;
        }

        public async Task<Entry> GetAsync(IKey key)
        {
            var entry = await _fhirStore.GetAsync(key).ConfigureAwait(false);
            if (entry != null)
            {
                _transfer.Externalize(entry);
            }
            return entry;
        }

        public Entry Add(Entry entry)
        {
            if (entry.State != EntryState.Internal)
            {
                _transfer.Internalize(entry);
            }
            _fhirStore.Add(entry);
            Entry result;
            if (entry.IsDelete)
            {
                result = entry;
            }
            else
            {
                result = _fhirStore.Get(entry.Key);
            }
            _transfer.Externalize(result);

            return result;
        }

        public async Task<Entry> AddAsync(Entry entry)
        {
            if (entry.State != EntryState.Internal)
            {
                _transfer.Internalize(entry);
            }
            await _fhirStore.AddAsync(entry).ConfigureAwait(false);
            Entry result;
            if (entry.IsDelete)
            {
                result = entry;
            }
            else
            {
                result = await _fhirStore.GetAsync(entry.Key).ConfigureAwait(false);
            }
            _transfer.Externalize(result);

            return result;
        }

        public IList<Entry> Get(IEnumerable<string> localIdentifiers, string sortby = null)
        {
            IList<Entry> results = _fhirStore.Get(localIdentifiers.Select(k => (IKey)Key.ParseOperationPath(k)));
            _transfer.Externalize(results);
            return results;
        }

        public async Task<IList<Entry>> GetAsync(IEnumerable<string> localIdentifiers, string sortby = null)
        {
            IList<Entry> results = await _fhirStore.GetAsync(localIdentifiers.Select(k => (IKey)Key.ParseOperationPath(k))).ConfigureAwait(false);
            _transfer.Externalize(results);
            return results;
        }
    }
}