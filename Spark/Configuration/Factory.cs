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
using System.Web;

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using MongoDB.Driver;
using Spark.Data.MongoDB;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Hl7.Fhir.Search;
using MongoDB.Bson;
using Spark.Search;
using Spark.Service;
using Spark.Config;
using Spark.Data.AmazonS3;
using System.Configuration;
using Spark.Core;

namespace Spark.Support
{
    
    public static class Factory
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

        public static IBlobStorage GetAmazonStorage()
        {
            // Create your own non public accounts file as "Spark/Accounts.config". See "Spark/Accounts.config.template"
            string accessKey = Settings.Get("AWSAccessKey");
            string secretKey = Settings.Get("AWSSecretKey");
            string bucketName = Settings.Get("AWSBucketName");

            return new AmazonS3Storage(accessKey, secretKey, bucketName);
        }

        public static ResourceImporter GetResourceImporter()
        {
            IFhirStore store = GetMongoFhirStore();
            ResourceImporter importer = new ResourceImporter(store, Settings.Endpoint);

            importer.SharedEndpoints.Add("http://hl7.org/fhir/");

            importer.SharedEndpoints.Add("localhost");
            importer.SharedEndpoints.Add("localhost.");

            return importer;
        }

        public static ResourceExporter GetResourceExporter()
        {
            return new ResourceExporter(Settings.Endpoint);
        }
        public static MongoFhirStore GetMongoFhirStore()
        {
            return new MongoFhirStore(MongoDbConnector.GetDatabase());
        }

        public static FhirMaintainanceService GetFhirMaintainceService()
        {
            FhirService service = new FhirService(new Uri(Settings.Endpoint, "maintainance")); // example: http://spark.furore.com/maintainance/
            return new FhirMaintainanceService(service);
        }
    }
}