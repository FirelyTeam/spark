using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using Spark.Store.Mongo;
using Spark.Engine.Model;
using Spark.Mongo.Search.Indexer;
using Spark.Engine.Interfaces;

namespace Spark.Mongo.Search.Common
{
    public class MongoIndexStore : IIndexStore
    {
        private MongoDatabase _database;
        private MongoIndexMapper _indexMapper;
        public MongoCollection<BsonDocument> Collection;

        public MongoIndexStore(string mongoUrl, MongoIndexMapper indexMapper)
        {
            _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            _indexMapper = indexMapper;
            Collection = _database.GetCollection(Config.MONGOINDEXCOLLECTION);
        }

        public void Save(IndexValue indexValue)
        {
            var result = _indexMapper.Map(indexValue);
            if (!result.IsBsonDocument)
            {
                throw new Exception("Expected BsonDocument as result, please check the mapping for IndexValue"); //Todo: make sure it always returns a document.
            }
            //result is like {"root" : {innerDocument}}, skip the root.
            //var innerResult = (result as BsonDocument).GetValue("root");
            //if (!innerResult.IsBsonDocument)
            //{
            //    throw new Exception("Expected BsonDocument as result, please check the mapping for IndexValue"); //Todo: make sure it always returns a document.
            //}

            Save(result as BsonDocument);
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
