/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */
using System.Collections.Generic;
using MongoDB.Driver;
using Spark.Core;
using Spark.Mongo.Search.Common;
using Hl7.Fhir.Model;

namespace Spark.Store.Mongo
{
    public static class MongoInfrastructureFactory
    {
        public static MongoFhirStore GetMongoFhirStore(MongoDatabase database)
        {
            return new MongoFhirStore(database);
        }

        public static MongoDatabase GetMongoDatabase(string url)
        {
            var mongourl = new MongoUrl(url);
            var client = new MongoClient(mongourl);
            return client.GetServer().GetDatabase(mongourl.DatabaseName);
        }

        public static MongoFhirIndex GetMongoFhirIndex(MongoDatabase database, IEnumerable<ModelInfo.SearchParamDefinition> searchparameters)
        {
            MongoIndexStore store = new MongoIndexStore(database);
            Definitions definitions = DefinitionsFactory.Generate(searchparameters);
            return new MongoFhirIndex(store, definitions);
        }

        public static Infrastructure AddMongo(this Infrastructure infrastructure, string url)
        {
            var database = GetMongoDatabase(url);
            var store = GetMongoFhirStore(database); 
            var index = GetMongoFhirIndex(database, infrastructure.SearchParameters);

            // MongoFhirStore implements three interfaces:
            infrastructure.Store = store;
            infrastructure.Generator = store;
            infrastructure.SnapshotStore = store;
            infrastructure.Index = index;

            return infrastructure;
        }

    }
}
