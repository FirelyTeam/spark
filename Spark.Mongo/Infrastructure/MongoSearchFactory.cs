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
    public class MongoSearchFactory
    {
        MongoDatabase database;

        public MongoSearchFactory(MongoDatabase database)
        {
            this.database = database;
        }

        private static Definitions _definitions;
        
        private static Definitions GetDefinitions()
        {
            if (_definitions == null) _definitions = DefinitionsFactory.GenerateFromMetadata();
            return _definitions;
        }

        public MongoFhirIndex CreateIndex()
        {
            MongoIndexStore store = new MongoIndexStore(database);
            Definitions definitions = DefinitionsFactory.GenerateFromMetadata();
            MongoFhirIndex index = new MongoFhirIndex(store, definitions);
            return index;
        }

        private static MongoFhirIndex _index;

        public static MongoFhirIndex GetIndex()
        {
            if (_index == null) _index = CreateIndex();
            return _index;
        }
    }
}
