/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
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

        public void Add(Entry entry)
        {
            BsonDocument document = SparkBsonHelper.ToBsonDocument(entry);
            Supercede(entry.Key);
            _collection.InsertOne(document);
        }

        public async Task AddAsync(Entry entry)
        {
            BsonDocument document = SparkBsonHelper.ToBsonDocument(entry);
            await SupercedeAsync(entry.Key).ConfigureAwait(false);
            await _collection.InsertOneAsync(document).ConfigureAwait(false);
        }

        public Entry Get(IKey key)
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

            return _collection.Find(query)
                .FirstOrDefault()
                ?.ToEntry();
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

        public IList<Entry> Get(IEnumerable<IKey> identifiers)
        {
            if (!identifiers.Any())
                return new List<Entry>();

            IList<IKey> identifiersList = identifiers.ToList();
            var versionedIdentifiers = GetBsonValues(identifiersList, k => k.HasVersionId());
            var unversionedIdentifiers = GetBsonValues(identifiersList, k => k.HasVersionId() == false);

            var queries = new List<FilterDefinition<BsonDocument>>();
            if (versionedIdentifiers.Any())
                queries.Add(GetSpecificVersionQuery(versionedIdentifiers));
            if (unversionedIdentifiers.Any())
                queries.Add(GetCurrentVersionQuery(unversionedIdentifiers));
            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.Or(queries);

            IEnumerable<BsonDocument> cursor = _collection.Find(query).ToEnumerable();

            return cursor.ToEntries().ToList();
        }


        public async Task<IList<Entry>> GetAsync(IEnumerable<IKey> identifiers)
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

            await _collection.Find(query)
                .ForEachAsync(doc =>
                {
                    result.Add(doc.ToEntry());
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

        private void Supercede(IKey key)
        {
            var pk = key.ToBsonReferenceKey();
            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq(Field.REFERENCE, pk),
                Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT)
            );

            UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set(Field.STATE, Value.SUPERCEDED);
            // A single delete on a sharded collection must contain an exact match on _id (and have the collection default collation) or contain the shard key (and have the simple collation). 
            _collection.UpdateMany(query, update);
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
