using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Search
{
    public class MongoIndexStore
    {
        MongoDatabase database;
        MongoCollection<BsonDocument> collection;

        public MongoIndexStore(MongoDatabase database)
        {
            this.database = database;
            this.collection = database.GetCollection(Spark.Search.Config.MONGOINDEXCOLLECTION);
        }

        public void Save(BsonDocument document)
        {
            string keyvalue = document.GetValue(InternalField.ID).ToString();
            IMongoQuery query = MongoDB.Driver.Builders.Query.EQ(InternalField.ID, keyvalue);

            // todo: should use Update: collection.Update();
            collection.Remove(query);
            collection.Save(document);
        }

        public void Delete(Interaction entry)
        {
            string location = entry.Key.ToRelativeUri().ToString();
            string id = entry.Key.RelativePath();
            IMongoQuery query = MongoDB.Driver.Builders.Query.EQ(InternalField.ID, id);
            collection.Remove(query);
        }

        public void Clean()
        {
            collection.RemoveAll();
        }

    }
}
