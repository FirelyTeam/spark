/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */
using MongoDB.Driver;
using Spark.Core;
using Spark.Data.AmazonS3;
using Spark.Service;
using Spark.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Store
{
    public class MongoStoreFactory
    {
        private MongoUrl url;
        private volatile MongoDatabase database;
        private volatile MongoFhirStore store;
        private object access = new Object();

        public MongoStoreFactory(string url)
        {
            this.url = new MongoUrl(url);
        }

        public MongoFhirStore GetMongoFhirStore()
        {
            var db = this.GetMongoDatabase();
            store = store ?? new MongoFhirStore(db);
            return store;
        }

        public MongoDatabase GetMongoDatabase()
        {
            if (database == null)
            {
                lock (access)
                {
                    if (database == null)
                    {
                        var client = new MongoClient(this.url);
                        database = client.GetServer().GetDatabase(url.DatabaseName);
                    }
                }
            }

            return database;
        }

        public FhirService MongoFhirService(Localhost localhost)
        {
            MongoFhirStore store = GetMongoFhirStore();
            return new FhirService(localhost, store, store, store);
        }

    }
}
