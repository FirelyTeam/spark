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
using Spark.Service;
using Spark.Data;
using Spark.Store;
using Spark.Support;
using Spark.Core;
using MongoDB.Driver;
using Spark.Data.AmazonS3;
using Hl7.Fhir.Model;

namespace Spark.Config
{

    public static class SparkDependencies
    {
        private static volatile object access = new object();
        private static bool registered = false;

        public static void Register()
        {
            lock (access)
            {
                if (!registered)
                {
                    registered = true;
                    MongoStoreFactory storefactory = new MongoStoreFactory(Settings.MongoUrl);

                    DependencyCoupler.Register<IFhirStore>(storefactory.GetMongoFhirStore);
                    DependencyCoupler.Register<ISnapshotStore>(storefactory.GetMongoFhirStore);
                    // DependencyCoupler.Register<ITagStore>(Spark.Store.MongoStoreFactory.GetMongoFhirStorage);
                    // DependencyCoupler.Register<IFhirIndex>(Spark.Search.MongoSearchFactory.GetIndex);
                    DependencyCoupler.Register<IGenerator>(storefactory.GetMongoFhirStore);
                   
                    // FhirService construction delayed until needed by using lambda
                    DependencyCoupler.Register<FhirService>(() => new FhirService(Settings.Endpoint)); 

                    DependencyCoupler.Register<ResourceImporter>(Factory.GetResourceImporter);
                    DependencyCoupler.Register<ResourceExporter>(Factory.GetResourceExporter);

                    if (Config.Settings.UseS3)
                    {
                        DependencyCoupler.Register<IBlobStorage>(new AmazonS3Storage(Settings.AwsAccessKey, Settings.AwsSecretKey, Settings.AwsBucketName));
                    }

                    DependencyCoupler.Register<Conformance>(Factory.GetSparkConformance);
                }
            }
        }
    }
}