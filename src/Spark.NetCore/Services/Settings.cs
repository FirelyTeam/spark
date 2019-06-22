using Spark.Engine;
using Spark.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spark.NetCore.Services
{
    public class Settings : ISettings
    {
        public SparkSettings SparkSettings { get; set; }
        public MongoStoreSettings MongoStoreSettings { get; set; }
    }
}
