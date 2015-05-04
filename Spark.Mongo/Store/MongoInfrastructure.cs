/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using Spark.Core;
using Spark.Mongo.AmazonS3;
using Spark.Service;
using Spark.MongoSearch;

namespace Spark.Mongo
{
    public static class MongoInfrastructure
    {
        public static MongoFhirStore GetMongoFhirStore(MongoDatabase database)
        {
            return new MongoFhirStore(database);
        }


        private static MongoDatabase GetMongoDatabase(string url)
        {
            var mongourl = new MongoUrl(url);
            var client = new MongoClient(mongourl);
            return client.GetServer().GetDatabase(mongourl.DatabaseName);
        }

        public static MongoFhirIndex GetMongoFhirIndex(MongoDatabase database)
        {
            MongoIndexStore store = new MongoIndexStore(database);
            Definitions definitions = DefinitionsFactory.GenerateFromMetadata();
            return new MongoFhirIndex(store, definitions);
        }

        public static Infrastructure AddMongo(this Infrastructure infrastructure, string url)
        {
            var database = GetMongoDatabase(url);
            var store = GetMongoFhirStore(database); // store has three interfaces
            var index = GetMongoFhirIndex(database);

            infrastructure.Store = store;
            infrastructure.Generator = store;
            infrastructure.SnapshotStore = store;
            infrastructure.Index = index;

            return infrastructure;
        }

    }
}
