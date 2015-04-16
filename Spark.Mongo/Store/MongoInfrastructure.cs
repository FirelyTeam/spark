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

namespace Spark.Mongo
{
    public static class MongoInfrastructure
    {
        public static MongoFhirStore GetMongoFhirStore(string url)
        {
            var database = GetMongoDatabase(url);
            return new MongoFhirStore(database);
        }

        public static MongoDatabase GetMongoDatabase(string url)
        {
            var mongourl = new MongoUrl(url);
            var client = new MongoClient(mongourl);
            return client.GetServer().GetDatabase(mongourl.DatabaseName);
        }

        public static Infrastructure AddMongo(this Infrastructure infrastructure, string url)
        {
            var store = GetMongoFhirStore(url); // has three interfaces
            infrastructure.Store = store;
            infrastructure.Generator = store;
            infrastructure.SnapshotStore = store;
            return infrastructure;
        }

    }
}
