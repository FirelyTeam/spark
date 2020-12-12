using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store
{
    using System.Threading.Tasks;

    public class MongoSnapshotStore : ISnapshotStore
    {
        private readonly IMongoDatabase database;
        public MongoSnapshotStore(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        }
        public async Task AddSnapshot(Snapshot snapshot)
        {
            var collection = database.GetCollection<Snapshot>(Collection.SNAPSHOT);
            await collection.InsertOneAsync(snapshot).ConfigureAwait(false);
        }

        public async Task<Snapshot> GetSnapshot(string snapshotid)
        {
            var collection = database.GetCollection<Snapshot>(Collection.SNAPSHOT);
            var results = await  collection.FindAsync(s => s.Id == snapshotid).ConfigureAwait(false);
            return results.FirstOrDefault();
        }
    }
}