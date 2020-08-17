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
            var collection = database.GetCollection<Snapshot>(Collection.SNAPSHOT);
            collection.InsertOne(snapshot);
        }

        public Snapshot GetSnapshot(string snapshotid)
        {
            var collection = database.GetCollection<Snapshot>(Collection.SNAPSHOT);
            return collection.Find(s => s.Id == snapshotid).FirstOrDefault();
        }
    }
}