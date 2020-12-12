using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Interfaces;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store
{
    using System.Threading.Tasks;

    public class MongoStoreAdministration : IFhirStoreAdministration
    {
        private readonly IMongoDatabase database;
        private readonly IMongoCollection<BsonDocument> collection;

        public MongoStoreAdministration(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            this.collection = database.GetCollection<BsonDocument>(Collection.RESOURCE);
        }
        public async Task Clean()
        {
            await EraseData().ConfigureAwait(false);
            await EnsureIndices().ConfigureAwait(false);
        }

        // Drops all collections, including the special 'counters' collection for generating ids,
        // AND the binaries stored at Amazon S3
        private Task EraseData()
        {
            // Don't try this at home
            var collectionsToDrop = new string[] { Collection.RESOURCE, Collection.COUNTERS, Collection.SNAPSHOT };
            return DropCollections(collectionsToDrop);
        }
        private async Task DropCollections(IEnumerable<string> collections)
        {
            foreach (var name in collections)
            {
                await TryDropCollection(name).ConfigureAwait(false);
            }
        }

        private Task EnsureIndices()
        {
            var indices = new List<CreateIndexModel<BsonDocument>>
            {
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(Field.STATE).Ascending(Field.METHOD).Ascending(Field.TYPENAME)),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(Field.PRIMARYKEY).Ascending(Field.STATE)),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Descending(Field.WHEN).Ascending(Field.TYPENAME)),
            };
            return collection.Indexes.CreateManyAsync(indices);
        }

        private Task TryDropCollection(string name)
        {
            try
            {
                return database.DropCollectionAsync(name);
            }
            catch
            {
                //don't worry. if it's not there. it's not there.
                return Task.CompletedTask;
            }
        }
    }
}