﻿/*
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
using Spark.Engine;
using Spark.Engine.Core;
using Spark.Engine.Search;
using Spark.Search.Mongo;
using Spark.Engine.Store.Interfaces;

namespace Spark.Mongo.Search.Common
{
    using System.Threading;
    using System.Threading.Tasks;

    public class MongoFhirIndex : IFhirIndex
    {
        private readonly MongoSearcher _searcher;
        private readonly IIndexStore _indexStore;
        private readonly SearchSettings _searchSettings;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public MongoFhirIndex(IIndexStore indexStore, MongoSearcher searcher, SparkSettings sparkSettings = null)
        {
            _indexStore = indexStore;
            _searcher = searcher;
            _searchSettings = sparkSettings?.Search ?? new SearchSettings();
        }

        public async Task Clean()
        {
            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);
                await _indexStore.Clean().ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task<SearchResults> Search(string resource, SearchParams searchCommand)
        {
            return _searcher.Search(resource, searchCommand, _searchSettings);
        }

        public async Task<Key> FindSingle(string resource, SearchParams searchCommand)
        {
            // todo: this needs optimization

            SearchResults results = await _searcher.Search(resource, searchCommand, _searchSettings).ConfigureAwait(false);
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
        public Task<SearchResults> GetReverseIncludes(IList<IKey> keys, IList<string> revIncludes)
        {
            return _searcher.GetReverseIncludes(keys, revIncludes);
        }

    }
}
