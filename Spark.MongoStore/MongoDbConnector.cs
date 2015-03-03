/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spark.Core;

namespace Spark.Store
{
    public sealed class MongoDbConnector
    {
        private static volatile MongoDatabase instance;
        private static object syncRoot = new Object();

        private MongoDbConnector() { }

        public static string ConnectionString
        {
            get
            { 
                var connectionstring = ConfigurationManager.AppSettings.Get("MONGOLAB_URI");
                return connectionstring;
            }
        }

        public static MongoDatabase Database
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            var url = new MongoUrl(ConnectionString);
                            var client = new MongoClient(url);
                            instance = client.GetServer().GetDatabase(url.DatabaseName);
                        }
                    }
                }

                return instance;
            }
        }

        public static MongoDatabase GetDatabase()
        {
            return Database;
        }


        //private static void initMappings()
        //{
        //    // We are now using our own Fhir json parsers and serializers to
        //    // serialize entries for use with Mongo. No mappings necessary anymore.

        //    if (!BsonClassMap.IsClassMapRegistered(typeof(BundleEntry)))
        //    {                               
        //        // New MongoDb client 1.8 syntax
        //        var conventionPack = new ConventionPack();
        //        conventionPack.Add(new IgnoreIfNullConvention(true));
        //        conventionPack.Add(new NamedIdMemberConvention("SelfLink"));

        //        // override any conventions you want to be different
        //        ConventionRegistry.Register("Fhir BundleEntry mapping", conventionPack, t => true);

        //        // Register subclasses of BundleEntry to deserializer knows how to find them
        //        var bundleEntryMap = BsonClassMap.RegisterClassMap<BundleEntry>();
        //        var resourceEntryMap = BsonClassMap.RegisterClassMap<ResourceEntry>();
        //        var binaryEntryMap = BsonClassMap.RegisterClassMap<BinaryEntry>();
        //        var deletedEntryMap = BsonClassMap.RegisterClassMap<DeletedEntry>();

        //        // Mongo should not store a BinaryEntry's byte array, this
        //        // is stored elsewhere in Amazon S3.
        //        // binaryEntryMap.UnmapProperty(be => be.Content);

        //        // Don't store the BundleEntry's Parent
        //        bundleEntryMap.UnmapProperty(be => be.Parent);

        //        // Register all subclasses of Resource and Element, so the deserializer knows how to find them
        //        Assembly fhirAssembly = typeof(Resource).Assembly;

        //        if (fhirAssembly != null)
        //        {
        //            //var resourceMap = BsonClassMap.RegisterClassMap<FhirModel.Resource>();
        //            //var dataMap = BsonClassMap.RegisterClassMap<FhirModel.Element>();

        //            foreach (var type in fhirAssembly.GetTypes())
        //            {
        //                if (type.ContainsGenericParameters)
        //                    continue;
        //                if (type.IsSubclassOf(typeof(Resource)) || type.IsSubclassOf(typeof(Element)))
        //                {
        //                    BsonClassMap.LookupClassMap(type);
        //                }
        //            }
        //        }
        //    }
        //}


        //    private static void GetServerAndDatabaseNames(out string server, out string database)
        //    {
        //        var connectionstring = ConfigurationManager.AppSettings.Get("MONGOHQ_URL");
        //        int separatorPosition = connectionstring.LastIndexOf('/');
        //        server = connectionstring.Substring(0, separatorPosition);
        //        database = connectionstring.Substring(separatorPosition + 1);
        //    }
    }
}
