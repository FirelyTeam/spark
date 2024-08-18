/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store.Extensions
{
    public class HistoryStore : IHistoryStore
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;

        public HistoryStore(string mongoUrl)
        {
            _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            _collection = _database.GetCollection<BsonDocument>(Collection.RESOURCE);
        }

        public async Task<Snapshot> HistoryAsync(string typename, HistoryParameters parameters)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>
            {
                Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, typename)
            };
            if (parameters.Since != null)
                clauses.Add(Builders<BsonDocument>.Filter.Gt(Field.WHEN, BsonDateTime.Create(parameters.Since)));

            return CreateSnapshot(await FetchPrimaryKeysAsync(clauses).ConfigureAwait(false), parameters.Count);
        }

        public async Task<Snapshot> HistoryAsync(IKey key, HistoryParameters parameters)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>
            {
                Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, key.TypeName),
                Builders<BsonDocument>.Filter.Eq(Field.RESOURCEID, key.ResourceId)
            };
            if (parameters.Since != null)
                clauses.Add(Builders<BsonDocument>.Filter.Gt(Field.WHEN, BsonDateTime.Create(parameters.Since)));

            return CreateSnapshot(await FetchPrimaryKeysAsync(clauses).ConfigureAwait(false), parameters.Count);
        }

        public async Task<Snapshot> HistoryAsync(HistoryParameters parameters)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>();
            if (parameters.Since != null)
                clauses.Add(Builders<BsonDocument>.Filter.Gt(Field.WHEN, BsonDateTime.Create(parameters.Since)));

            return CreateSnapshot(await FetchPrimaryKeysAsync(clauses).ConfigureAwait(false), parameters.Count);
        }

        private async Task<IReadOnlyList<string>> FetchPrimaryKeysAsync(IList<FilterDefinition<BsonDocument>> clauses)
        {
            var result = new List<string>();

            var query = clauses.Any()
                ? Builders<BsonDocument>.Filter.And(clauses)
                : Builders<BsonDocument>.Filter.Empty;

            await _collection.Find(query)
                .Project(Builders<BsonDocument>.Projection.Include(Field.PRIMARYKEY))
                .Sort(Builders<BsonDocument>.Sort.Descending(Field.WHEN))
                .ForEachAsync(doc =>
                {
                    result.Add(doc.GetValue(Field.PRIMARYKEY).AsString);
                });

            return result;
        }

        private static Snapshot CreateSnapshot(IReadOnlyList<string> keys, int? count = null, IReadOnlyList<string> includes = null, IReadOnlyList<string> reverseIncludes = null, IReadOnlyList<string> elements = null)
        {
            var link = new Uri(TransactionBuilder.HISTORY, UriKind.Relative);
            var snapshot = Snapshot.Create(Bundle.BundleType.History, link, keys, "history", count, includes, reverseIncludes, elements);
            return snapshot;
        }
    }
}