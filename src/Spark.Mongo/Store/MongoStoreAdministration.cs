using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Interfaces;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store
{
    public class MongoStoreAdministration : IFhirStoreAdministration
    {
        IMongoDatabase database;
        IMongoCollection<BsonDocument> collection;

        public MongoStoreAdministration(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            this.collection = database.GetCollection<BsonDocument>(Collection.RESOURCE);
        }
        public void Clean()
        {
            EraseData();
            EnsureIndices();
        }

        // Drops all collections, including the special 'counters' collection for generating ids,
        // AND the binaries stored at Amazon S3
        private void EraseData()
        {
            // Don't try this at home
            var collectionsToDrop = new string[] { Collection.RESOURCE, Collection.COUNTERS, Collection.SNAPSHOT };
            DropCollections(collectionsToDrop);

            /*
            // When using Amazon S3, remove blobs from there as well
            if (Config.Settings.UseS3)
            {
                using (var blobStorage = getBlobStorage())
                {
                    if (blobStorage != null)
                    {
                        blobStorage.Open();
                        blobStorage.DeleteAll();
                        blobStorage.Close();
                    }
                }
            }
            */
        }
        private void DropCollections(IEnumerable<string> collections)
        {
            foreach (var name in collections)
            {
                TryDropCollection(name);
            }
        }



        private void EnsureIndices()
        {
            var indices = new List<CreateIndexModel<BsonDocument>>
            {
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(Field.STATE).Ascending(Field.METHOD).Ascending(Field.TYPENAME)),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(Field.PRIMARYKEY).Ascending(Field.STATE)),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Descending(Field.WHEN).Ascending(Field.TYPENAME)),
            };
            collection.Indexes.CreateMany(indices);
        }

        private void TryDropCollection(string name)
        {
            try
            {
                database.DropCollection(name);
            }
            catch
            {
                //don't worry. if it's not there. it's not there.
            }
        }
    }
}