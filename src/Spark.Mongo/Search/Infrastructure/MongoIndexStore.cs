using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using Spark.Store.Mongo;
using Spark.Engine.Model;

namespace Spark.Mongo.Search.Common
{
    public class MongoIndexStore
    {
        private MongoDatabase database;
        public MongoCollection<BsonDocument> Collection;

        public MongoIndexStore(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            this.Collection = database.GetCollection(Config.MONGOINDEXCOLLECTION);
        }

        public void Save(IndexEntry indexEntry)
        {
            BsonDocument toInsert = new BsonDocument();
            foreach (var iv in indexEntry.Parts)
            {
                //convert every IndexValue to a nested BsonDocument or BsonElement.
            }

        }

        public void Save(BsonDocument document)
        {
            try
            {
                string keyvalue = document.GetValue(InternalField.ID).ToString();
                IMongoQuery query = MongoDB.Driver.Builders.Query.EQ(InternalField.ID, keyvalue);

                // todo: should use Update: collection.Update();
                Collection.Remove(query);
                Collection.Insert(document);
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public void Delete(Interaction entry)
        {
            string id = entry.Key.WithoutVersion().ToOperationPath();
            IMongoQuery query = MongoDB.Driver.Builders.Query.EQ(InternalField.ID, id);
            Collection.Remove(query);
        }

        public void Clean()
        {
            Collection.RemoveAll();
        }

    }
}
