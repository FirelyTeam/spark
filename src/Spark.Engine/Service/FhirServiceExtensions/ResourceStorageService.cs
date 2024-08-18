/* 
 * Copyright (c) 2016-2018, Firely (info@fire.ly)
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
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

        public async Task<Entry> GetAsync(IKey key)
        {
            var entry = await _fhirStore.GetAsync(key).ConfigureAwait(false);
            if (entry != null)
            {
                _transfer.Externalize(entry);
            }
            return entry;
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

        public async Task<IList<Entry>> GetAsync(IEnumerable<string> localIdentifiers, string sortby = null)
        {
            IList<Entry> results = await _fhirStore.GetAsync(localIdentifiers.Select(k => (IKey)Key.ParseOperationPath(k))).ConfigureAwait(false);
            _transfer.Externalize(results);
            return results;
        }
    }
}