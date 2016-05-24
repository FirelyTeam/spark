using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store
{
    public class MongoSnapshotStore : ISnapshotStore
    {
        MongoDatabase database;
        public MongoSnapshotStore(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        }
        public void AddSnapshot(Snapshot snapshot)
        {
            var collection = database.GetCollection(Collection.SNAPSHOT);
            collection.Save<Snapshot>(snapshot);
        }

        public Snapshot GetSnapshot(string snapshotid)
        {
            var collection = database.GetCollection(Collection.SNAPSHOT);
            return collection.FindOneByIdAs<Snapshot>(snapshotid);
        }
    }
}