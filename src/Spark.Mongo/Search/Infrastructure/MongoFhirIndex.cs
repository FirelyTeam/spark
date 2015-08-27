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

namespace Spark.Mongo.Search.Common
{
    public class MongoFhirIndex : IFhirIndex
    {
        private Definitions definitions;
        private MongoSearcher searcher;
        private MongoIndexer indexer;
        private MongoIndexStore indexStore;


        public MongoFhirIndex(MongoIndexStore store, Definitions definitions)
        {
            this.definitions = definitions;
            this.indexStore = store;
            this.indexer = new MongoIndexer(store, definitions);
            this.searcher = new MongoSearcher(store.Collection);
        }

        private object transaction = new object();
       
        public void Clean()
        {
            lock (transaction)
            {
                indexStore.Clean();
            }
        }

        public SearchResults Search(Spark.Search.Mongo.Parameters parameters)
        {
            return searcher.Search(parameters);
        }

        public SearchResults Search(string resource, string query = "")
        {
            var parameters = ParameterFactory.Parameters(definitions, resource, query);
            return searcher.Search(parameters);
        }

        public SearchResults Search(string resource, SearchParams searchCommand)
        {
            return searcher.Search(resource, searchCommand);
        }

        public Key FindSingle(string resource, SearchParams searchCommand)
        {
            // todo: this needs optimization

            SearchResults results = searcher.Search(resource, searchCommand);
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

        public void Process(IEnumerable<Interaction> interactions)
        {
            foreach (var i in interactions)
            {
                Process(i);
            }
        }

        public void Process(Interaction interaction)
        {
            indexer.Process(interaction);
        }

    }
}
