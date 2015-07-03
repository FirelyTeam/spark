using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Spark.Store.Mongo;

namespace Spark.Configuration
{
    public static class Infra
    {
        public static Infrastructure Mongo;

        static Infra()
        {
            Mongo = Infrastructure.Default();
            Mongo.AddLocalhost(Settings.Endpoint);
            Mongo.AddMongo(Settings.MongoUrl);
        }

        
    }
}