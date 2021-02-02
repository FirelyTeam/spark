using System;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using System.Threading.Tasks;
using Spark.Store.Mongo;
using Spark.Engine.Model;
using Spark.Mongo.Search.Indexer;
using Spark.Engine.Store.Interfaces;

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

        [Obsolete("Use Async method version instead")]
        public void Save(IndexValue indexValue)
        {
            Task.Run(() => SaveAsync(indexValue)).GetAwaiter().GetResult();
        }

        [Obsolete("Use Async method version instead")]
        public void Delete(Entry entry)
        {
            Task.Run(() => DeleteAsync(entry)).GetAwaiter().GetResult();
        }

        [Obsolete("Use Async method version instead")]
        public void Clean()
        {
            Task.Run(() => CleanAsync()).GetAwaiter().GetResult();
        }

        public async Task SaveAsync(IndexValue indexValue)
        {
            var result = _indexMapper.MapEntry(indexValue);

            foreach (var doc in result)
            {
                await SaveAsync(doc).ConfigureAwait(false);
            }
        }

        public async Task SaveAsync(BsonDocument document)
        {
            string keyvalue = document.GetValue(InternalField.ID).ToString();
            var query = Builders<BsonDocument>.Filter.Eq(InternalField.ID, keyvalue);

            // todo: should use Update: collection.Update();
            await Collection.DeleteManyAsync(query).ConfigureAwait(false);
            await Collection.InsertOneAsync(document).ConfigureAwait(false);
        }

        public async Task DeleteAsync(Entry entry)
        {
            string id = entry.Key.WithoutVersion().ToOperationPath();
            var query = Builders<BsonDocument>.Filter.Eq(InternalField.ID, id);
            await Collection.DeleteManyAsync(query).ConfigureAwait(false);
        }

        public async Task CleanAsync()
        {
            await Collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty).ConfigureAwait(false);
        }
    }
}
