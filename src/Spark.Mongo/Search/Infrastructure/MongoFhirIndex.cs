/*
 * Copyright (c) 2014-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2019-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spark.Core;
using Hl7.Fhir.Rest;
using Spark.Engine;
using Spark.Engine.Core;
using Spark.Engine.Search;
using Spark.Search.Mongo;
using Spark.Engine.Store.Interfaces;

namespace Spark.Mongo.Search.Common
{
    public class MongoFhirIndex : IFhirIndex
    {
        private MongoSearcher _searcher;
        private IIndexStore _indexStore;
        private SearchSettings _searchSettings;

        public MongoFhirIndex(IIndexStore indexStore, MongoSearcher searcher, SparkSettings sparkSettings = null)
        {
            _indexStore = indexStore;
            _searcher = searcher;
            _searchSettings = sparkSettings?.Search ?? new SearchSettings();
        }

        private readonly SemaphoreSlim _transaction = new SemaphoreSlim(1, 1);

        public async Task CleanAsync()
        {
            await _transaction.WaitAsync().ConfigureAwait(false);
            try
            {
                await _indexStore.CleanAsync().ConfigureAwait(false);
            }
            finally
            {
                _transaction.Release();
            }
        }

        public async Task<SearchResults> SearchAsync(string resource, SearchParams searchCommand)
        {
            return await _searcher.SearchAsync(resource, searchCommand, _searchSettings).ConfigureAwait(false);
        }

        public async Task<Key> FindSingleAsync(string resource, SearchParams searchCommand)
        {
            // todo: this needs optimization

            var results = await _searcher.SearchAsync(resource, searchCommand, _searchSettings).ConfigureAwait(false);
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

        public async Task<SearchResults> GetReverseIncludesAsync(IList<IKey> keys, IList<string> revIncludes)
        {
            return await _searcher.GetReverseIncludesAsync(keys, revIncludes).ConfigureAwait(false);
        }
    }
}
