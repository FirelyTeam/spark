using System.Threading.Tasks;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store
{
    public class MongoSnapshotStore : ISnapshotStore
    {
        IMongoDatabase database;

        public MongoSnapshotStore(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        }

        public void AddSnapshot(Snapshot snapshot)
        {
            Task.Run(() => AddSnapshotAsync(snapshot)).GetAwaiter().GetResult();
        }

        public Snapshot GetSnapshot(string snapshotid)
        {
            return Task.Run(() => GetSnapshotAsync(snapshotid)).GetAwaiter().GetResult();
        }

        public async Task AddSnapshotAsync(Snapshot snapshot)
        {
            var collection = database.GetCollection<Snapshot>(Collection.SNAPSHOT);
            await collection.InsertOneAsync(snapshot).ConfigureAwait(false);
        }

        public async Task<Snapshot> GetSnapshotAsync(string snapshotId)
        {
            var collection = database.GetCollection<Snapshot>(Collection.SNAPSHOT);
            return (await collection.FindAsync(s => s.Id == snapshotId).ConfigureAwait(false))
                .FirstOrDefault();
        }
    }
}