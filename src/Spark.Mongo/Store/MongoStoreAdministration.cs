﻿/*
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021-2023, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Interfaces;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store
{
    public class MongoStoreAdministration : IFhirStoreAdministration
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;

        public MongoStoreAdministration(string mongoUrl)
        {
            _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            _collection = _database.GetCollection<BsonDocument>(Collection.RESOURCE);
        }

        public async Task CleanAsync()
        {
            await EraseDataAsync().ConfigureAwait(false);
            await EnsureIndicesAsync().ConfigureAwait(false);
        }

        // Drops all collections, including the special 'counters' collection for generating ids,
        // AND the binaries stored at Amazon S3
        private async Task EraseDataAsync()
        {
            // Don't try this at home
            var collectionsToDrop = new string[] { Collection.RESOURCE, Collection.COUNTERS, Collection.SNAPSHOT };
            await DropCollectionsAsync(collectionsToDrop).ConfigureAwait(false);
        }

        private async Task DropCollectionsAsync(IEnumerable<string> collections)
        {
            foreach (var name in collections)
            {
                await TryDropCollectionAsync(name).ConfigureAwait(false);
            }
        }

        private async Task EnsureIndicesAsync()
        {
            var indices = new List<CreateIndexModel<BsonDocument>>
            {
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(Field.STATE).Ascending(Field.METHOD).Ascending(Field.TYPENAME)),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(Field.PRIMARYKEY).Ascending(Field.STATE)),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Descending(Field.WHEN).Ascending(Field.TYPENAME)),
            };
            await _collection.Indexes.CreateManyAsync(indices).ConfigureAwait(false);
        }

        private async Task TryDropCollectionAsync(string name)
        {
            try
            {
                await _database.DropCollectionAsync(name).ConfigureAwait(false);
            }
            catch
            {
                //don't worry. if it's not there. it's not there.
            }
        }
    }
}