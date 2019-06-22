using Spark.Engine;
using Spark.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spark.NetCore.Services
{
    public interface ISettings
    {
        SparkSettings SparkSettings { get; set; }
        MongoStoreSettings MongoStoreSettings { get; set; }
    }
}
