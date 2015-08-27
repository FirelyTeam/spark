using Spark.Core;
using Spark.Store.Mongo;

namespace Spark
{
    public static class InfrastructureProvider
    {
        public static Infrastructure Mongo;

        static InfrastructureProvider()
        {
            Mongo = Infrastructure.Default();
            Mongo.AddLocalhost(Settings.Endpoint);
            Mongo.AddMongo(Settings.MongoUrl);
        }

    }
}