﻿using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Interfaces;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store
{
    public class MongoStoreAdministration : IFhirStoreAdministration
    {
        MongoDatabase database;
        MongoCollection<BsonDocument> collection;

        public MongoStoreAdministration(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            this.collection = database.GetCollection(Collection.RESOURCE);
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
            try
            {
                collection.CreateIndex(Field.STATE, Field.METHOD, Field.TYPENAME);
                collection.CreateIndex(Field.PRIMARYKEY, Field.STATE);
                var index = MongoDB.Driver.Builders.IndexKeys.Descending(Field.WHEN).Ascending(Field.TYPENAME);
                collection.CreateIndex(index);
            }
            catch (System.Exception ex)
            {
                // With Azure this index isn't needed anyway!
            }
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