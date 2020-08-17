using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Store.Mongo
{
    public static class MongoDatabaseFactory
    {
        private static Dictionary<string, IMongoDatabase> instances;

        public static IMongoDatabase GetMongoDatabase(string url)
        {
            IMongoDatabase result;
        
            if (instances == null) //instances dictionary is not at all initialized
            {
                instances = new Dictionary<string, IMongoDatabase>();
            }
            if (instances.Where(i => i.Key == url).Count() == 0) //there is no instance for this url yet
            {
                result = CreateMongoDatabase(url);
                instances.Add(url, result);
            }; 
            return instances.First(i => i.Key == url).Value; //now there must be one.
        }

        private static IMongoDatabase CreateMongoDatabase(string url)
        {
            var mongourl = new MongoUrl(url);
            var client = new MongoClient(mongourl);
            return client.GetDatabase(mongourl.DatabaseName);
        }
    }
}
