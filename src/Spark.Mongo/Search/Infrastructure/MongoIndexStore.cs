using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Extensions;

namespace Spark.Mongo.Search.Common
{
    public class MongoIndexStore
    {
        MongoDatabase database;
        public MongoCollection<BsonDocument> Collection;

        public MongoIndexStore(MongoDatabase database)
        {
            this.database = database;
            this.Collection = database.GetCollection(Config.MONGOINDEXCOLLECTION);
        }

        public void Save(BsonDocument document)
        {
            string keyvalue = document.GetValue(InternalField.ID).ToString();
            IMongoQuery query = MongoDB.Driver.Builders.Query.EQ(InternalField.ID, keyvalue);

            // todo: should use Update: collection.Update();
            Collection.Remove(query);
            Collection.Save(document);
        }

        public void Delete(Interaction entry)
        {
            string location = entry.Key.ToRelativeUri().ToString();
            string id = entry.Key.ToOperationPath();
            IMongoQuery query = MongoDB.Driver.Builders.Query.EQ(InternalField.ID, id);
            Collection.Remove(query);
        }

        public void Clean()
        {
            Collection.RemoveAll();
        }

    }
}
