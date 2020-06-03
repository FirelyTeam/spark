/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Collections.Generic;
using System.Linq;
using Spark.Core;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Search.Mongo;
using Spark.Engine.Store.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Spark.Mongo.Search.Common
{
    public class MongoFhirIndex : IFhirIndex
    {
        private MongoSearcher _searcher;
        private MongoIndexer _indexer;
        private IIndexStore _indexStore;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public MongoFhirIndex(IIndexStore indexStore, MongoIndexer indexer, MongoSearcher searcher)
        {
            _indexStore = indexStore;
            _indexer = indexer;
            _searcher = searcher;
        }

        public async Task Clean()
        {
            await _semaphore.WaitAsync();
            await _indexStore.Clean();
            _semaphore.Release();
        }

        public async Task<SearchResults> Search(string resource, SearchParams searchCommand)
        {
            return await _searcher.Search(resource, searchCommand);
        }

        public async Task<Key> FindSingle(string resource, SearchParams searchCommand)
        {
            // todo: this needs optimization
            SearchResults results = await _searcher.Search(resource, searchCommand);
            if (results.Count > 1)
            {
                throw Error.BadRequest("The search for a single resource yielded more than one.");
            }
            else if (results.Count == 0)
            {
                throw Error.BadRequest("No resources were found while searching for a single resource.");
            }
            else
            {
                string location = results.FirstOrDefault();
                return Key.ParseOperationPath(location);
            }
        }

        public async Task Process(IEnumerable<Entry> entry)
        {
            foreach (var i in entry)
            {
                await Process(i);
            }
        }

        public Task Process(Entry entry)
        {
            return _indexer.Process(entry);
        }

        public Task<SearchResults> GetReverseIncludes(IList<IKey> keys, IList<string> revIncludes)
        {
            return _searcher.GetReverseIncludes(keys, revIncludes);
        }
    }
}
