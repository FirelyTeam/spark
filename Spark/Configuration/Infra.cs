using Spark.App;
using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Spark.Mongo;

namespace Spark.App
{
    public static class Infra
    {
        static Infra()
        {
            Infra.Mongo = new Infrastructure().AddLocalhost(Settings.Endpoint).AddMongo(Settings.MongoUrl);
        }

        public static Infrastructure Mongo;
    }
}