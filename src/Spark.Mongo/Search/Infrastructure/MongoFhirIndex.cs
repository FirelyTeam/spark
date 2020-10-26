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

        private object transaction = new object();
       
        public void Clean()
        {
            lock (transaction)
            {
                _indexStore.Clean();
            }
        }

        public SearchResults Search(string resource, SearchParams searchCommand)
        {
            return _searcher.Search(resource, searchCommand, _searchSettings);
        }

        public Key FindSingle(string resource, SearchParams searchCommand)
        {
            // todo: this needs optimization

            SearchResults results = _searcher.Search(resource, searchCommand, _searchSettings);
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
        public SearchResults GetReverseIncludes(IList<IKey> keys, IList<string> revIncludes)
        {
            return _searcher.GetReverseIncludes(keys, revIncludes);
        }

    }
}
