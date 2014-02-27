using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Spark.Service;
using Spark.Search;
using Spark.Data;
using Spark.Data.MongoDB;
using Spark.Support;
using Spark.Core;
using MongoDB.Driver;

namespace Spark.Config
{

    public static class Dependencies
    {
        public static void Register()
        {
            DependencyCoupler.Register<IFhirService>(
                delegate()
                {
                    return new FhirService(Settings.Endpoint);
                });
            DependencyCoupler.Register<IFhirStore>(Factory.GetMongoFhirStore);
            DependencyCoupler.Register<IFhirIndex>(Factory.GetIndex);
               
            DependencyCoupler.Register<IIndexer, MongoIndexer>();

            DependencyCoupler.Register<ResourceImporter>(Factory.GetResourceImporter);

            DependencyCoupler.Register<ResourceExporter>(Factory.GetResourceExporter);

            DependencyCoupler.Register<IBlobStorage>(Factory.GetAmazonStorage);

            DependencyCoupler.Register<MongoDatabase>(MongoDbConnector.GetDatabase);
        }
    }
}