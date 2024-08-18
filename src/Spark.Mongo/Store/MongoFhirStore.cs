/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;

namespace Spark.Store.Mongo
{

    public class MongoFhirStore : IFhirStore
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;

        public MongoFhirStore(string mongoUrl)
        {
            _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            _collection = _database.GetCollection<BsonDocument>(Collection.RESOURCE);
        }

        public async Task AddAsync(Entry entry)
        {
            BsonDocument document = SparkBsonHelper.ToBsonDocument(entry);
            await SupercedeAsync(entry.Key).ConfigureAwait(false);
            await _collection.InsertOneAsync(document).ConfigureAwait(false);
        }

        public async Task<Entry> GetAsync(IKey key)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>
            {
                Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, key.TypeName),
                Builders<BsonDocument>.Filter.Eq(Field.RESOURCEID, key.ResourceId)
            };

            if (key.HasVersionId())
            {
                clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.VERSIONID, key.VersionId));
            }
            else
            {
                clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT));
            }

            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.And(clauses);

            return (await _collection.Find(query)
                .FirstOrDefaultAsync())
                ?.ToEntry();
        }

        private IFindFluent<BsonDocument, BsonDocument> AddProjection(IFindFluent<BsonDocument, BsonDocument> queryable, IEnumerable<string> elements)
        {
            if (elements != null && elements.Any())
            {
                // add metadata
                var projection = Builders<BsonDocument>.Projection
                .Include(Field.PRIMARYKEY)
                .Include(Field.REFERENCE)
                .Include(Field.WHEN)
                .Include(Field.STATE)
                .Include(Field.VERSIONID)
                .Include(Field.TYPENAME)
                .Include(Field.METHOD)
                .Include(Field.TRANSACTION)
                .Include(Field.RESOURCEID)
                .Include(Field.RESOURCETYPE);

                // add elements
                foreach (var element in elements)
                {
                    projection = projection
                        .Include(element)
                        // add element extension
                        .Include($"_{element}");
                }

                queryable = queryable.Project(projection);
            }

            return queryable;
        }

        public async Task<IList<Entry>> GetAsync(IEnumerable<IKey> identifiers, IEnumerable<string> elements)
        {
            var result = new List<Entry>();

            if (!identifiers.Any())
                return result;

            IList<IKey> identifiersList = identifiers.ToList();
            var versionedIdentifiers = GetBsonValues(identifiersList, k => k.HasVersionId());
            var unversionedIdentifiers = GetBsonValues(identifiersList, k => k.HasVersionId() == false);

            var queries = new List<FilterDefinition<BsonDocument>>();
            if (versionedIdentifiers.Any())
                queries.Add(GetSpecificVersionQuery(versionedIdentifiers));
            if (unversionedIdentifiers.Any())
                queries.Add(GetCurrentVersionQuery(unversionedIdentifiers));
            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.Or(queries);

            var queryable = _collection.Find(query);
            queryable = AddProjection(queryable, elements);
            var subsetted = elements != null && elements.Any();
            await queryable
                .ForEachAsync(doc =>
                {
                    result.Add(doc.ToEntry(subsetted));
                });
            
            return result;
        }

        private IEnumerable<BsonValue> GetBsonValues(IEnumerable<IKey> identifiers, Func<IKey, bool> keyCondition)
        {
            return identifiers.Where(keyCondition).Select(k => (BsonValue)k.ToString());
        }

        private FilterDefinition<BsonDocument> GetCurrentVersionQuery(IEnumerable<BsonValue> ids)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>
            {
                Builders<BsonDocument>.Filter.In(Field.REFERENCE, ids),
                Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT)
            };
            return Builders<BsonDocument>.Filter.And(clauses);
        }

        private FilterDefinition<BsonDocument> GetSpecificVersionQuery(IEnumerable<BsonValue> ids)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>
            {
                Builders<BsonDocument>.Filter.In(Field.PRIMARYKEY, ids)
            };

            return Builders<BsonDocument>.Filter.And(clauses);
        }

        private async Task SupercedeAsync(IKey key)
        {
            var pk = key.ToBsonReferenceKey();
            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq(Field.REFERENCE, pk),
                Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT)
            );

            UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set(Field.STATE, Value.SUPERCEDED);
            // A single delete on a sharded collection must contain an exact match on _id (and have the collection default collation) or contain the shard key (and have the simple collation). 
            await _collection.UpdateManyAsync(query, update);
        }
    }
}
