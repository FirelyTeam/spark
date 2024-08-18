/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Store.Mongo;
using System.Threading.Tasks;

namespace Spark.Mongo.Store
{
    public class MongoFhirStorePagedReader : IFhirStorePagedReader
    {
        private readonly IMongoCollection<BsonDocument> _collection;

        public MongoFhirStorePagedReader(string mongoUrl)
        {
            var database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            _collection = database.GetCollection<BsonDocument>(Collection.RESOURCE);
        }

        public async Task<IPageResult<Entry>> ReadAsync(FhirStorePageReaderOptions options)
        {
            options = options ?? new FhirStorePageReaderOptions();

            var filter = Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT);

            var totalRecords = await _collection.CountDocumentsAsync(filter)
                .ConfigureAwait(false);

            return new MongoCollectionPageResult<Entry>(_collection, filter,
                options.PageSize, totalRecords,
                document => document.ToEntry());
        }
    }
}
