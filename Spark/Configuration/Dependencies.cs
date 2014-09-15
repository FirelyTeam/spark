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
using Spark.Search;
using Spark.Data;
using Spark.Store;
using Spark.Support;
using Spark.Core;
using MongoDB.Driver;
using Spark.Mongo.Utils;

namespace Spark.Config
{

    public static class Dependencies
    {
        private static bool registered = false;
        public static void Register()
        {
            if (!registered)
            {
                registered = true;

                DependencyCoupler.Register<IFhirService>(
                    delegate()
                    {
                        return new FhirService(Settings.Endpoint);
                    });
                DependencyCoupler.Register<IFhirStore>(Spark.Store.MongoStoreFactory.GetMongoFhirStore);
                DependencyCoupler.Register<IFhirIndex>(Spark.Search.MongoSearchFactory.GetIndex);

               
                DependencyCoupler.Register<ResourceImporter>(Factory.GetResourceImporter);

                DependencyCoupler.Register<ResourceExporter>(Factory.GetResourceExporter);

                if (Config.Settings.UseS3)
                {
                    DependencyCoupler.Register<IBlobStorage>(Spark.Store.MongoStoreFactory.GetAmazonStorage);
                }

                DependencyCoupler.Register<MongoDatabase>(MongoDbConnector.GetDatabase);
            }
        }
    }
}