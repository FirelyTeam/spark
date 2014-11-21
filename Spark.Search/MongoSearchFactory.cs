/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Search
{
    public static class MongoSearchFactory
    {
        private static Definitions definitions;
        public static Definitions Definitions
        {
            get
            {
                if (definitions == null)
                    definitions = DefinitionsFactory.GenerateFromMetadata();
                return definitions;
            }
        }

       

        public static FhirIndex CreateIndex()
        {
            MongoDatabase database = MongoDbConnector.Database;
            MongoCollection<BsonDocument> collection = database.GetCollection(Spark.Search.Config.MONGOINDEXCOLLECTION);

            Definitions definitions = DefinitionsFactory.GenerateFromMetadata();
            ISearcher searcher = new MongoSearcher(collection);
            IIndexer indexer = new MongoIndexer(collection, definitions);

            FhirIndex index = new FhirIndex(definitions, indexer, searcher);
            return index;
        }

        private static FhirIndex index;
        public static FhirIndex GetIndex()
        {
            if (index == null)
                index = CreateIndex();
            return index;
        }
    }
}
