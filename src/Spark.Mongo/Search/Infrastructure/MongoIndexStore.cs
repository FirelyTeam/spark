﻿using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using System;
using Spark.Store.Mongo;
using Spark.Engine.Model;
using Spark.Mongo.Search.Indexer;
using Spark.Engine.Store.Interfaces;
using System.Threading.Tasks;

namespace Spark.Mongo.Search.Common
{
    public class MongoIndexStore : IIndexStore
    {
        private IMongoDatabase _database;
        private MongoIndexMapper _indexMapper;
        public IMongoCollection<BsonDocument> Collection;

        public MongoIndexStore(string mongoUrl, MongoIndexMapper indexMapper)
        {
            _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            _indexMapper = indexMapper; 
            Collection = _database.GetCollection<BsonDocument>(Config.MONGOINDEXCOLLECTION);
        }

        public async Task Save(IndexValue indexValue)
        {
            var result = _indexMapper.MapEntry(indexValue);

            foreach (var doc in result)
            {
                await Save(doc);
            }
        }

        public async Task Save(BsonDocument document)
        {
            try
            {
                string keyvalue = document.GetValue(InternalField.ID).ToString();
                var query = Builders<BsonDocument>.Filter.Eq(InternalField.ID, keyvalue);

                // todo: should use Update: collection.Update();
                await Collection.DeleteManyAsync(query);
                await Collection.InsertOneAsync(document);
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public Task Delete(Entry entry)
        {
            string id = entry.Key.WithoutVersion().ToOperationPath();
            var query = Builders<BsonDocument>.Filter.Eq(InternalField.ID, id);
            return Collection.DeleteManyAsync(query);
        }

        public Task Clean()
        {
            return Collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
        }
    }
}
