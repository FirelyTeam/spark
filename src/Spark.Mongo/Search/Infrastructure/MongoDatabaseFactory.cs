using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;

namespace Spark.Store.Mongo
{
    public static class MongoDatabaseFactory
    {
        private static Dictionary<string, IMongoDatabase> _instances = new Dictionary<string, IMongoDatabase>();
        private static readonly object _locker = new object();

        public static IMongoDatabase GetMongoDatabase(string url)
        {
            if (_instances.Where(i => i.Key == url).Count() == 0) //there is no instance for this url yet
            {
                lock (_locker)
                {
                    if (_instances.Where(i => i.Key == url).Count() == 0)
                    {
                        var result = CreateMongoDatabase(url);
                        _instances.Add(url, result);
                    }
                }
            };

            return _instances.First(i => i.Key == url).Value; //now there must be one.
        }

        private static IMongoDatabase CreateMongoDatabase(string url)
        {
            var mongourl = new MongoUrl(url);
            var client = new MongoClient(mongourl);
            return client.GetDatabase(mongourl.DatabaseName);
        }
    }
}
