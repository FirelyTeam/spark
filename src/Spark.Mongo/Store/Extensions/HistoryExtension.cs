using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Auxiliary;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store.Extensions
{
    using System.Threading.Tasks;

    public class HistoryStore : IHistoryStore
    {
        private readonly IMongoDatabase database;
        private readonly IMongoCollection<BsonDocument> collection;
        public HistoryStore(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            this.collection = database.GetCollection<BsonDocument>(Collection.RESOURCE);
        }

        public async Task<Snapshot> History(string resource, HistoryParameters parameters)
        {
            var clauses =
                new List<FilterDefinition<BsonDocument>> {Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, resource)};

            if (parameters.Since != null)
                clauses.Add(Builders<BsonDocument>.Filter.Gt(Field.WHEN, BsonDateTime.Create(parameters.Since)));

            var primaryKeys = await FetchPrimaryKeys(clauses).ConfigureAwait(false);
            return CreateSnapshot(primaryKeys, parameters.Count);
        }

        public async Task<Snapshot> History(IKey key, HistoryParameters parameters)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>
            {
                Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, key.TypeName),
                Builders<BsonDocument>.Filter.Eq(Field.RESOURCEID, key.ResourceId)
            };

            if (parameters.Since != null)
                clauses.Add(Builders<BsonDocument>.Filter.Gt(Field.WHEN, BsonDateTime.Create(parameters.Since)));

            var primaryKeys = await FetchPrimaryKeys(clauses).ConfigureAwait(false);
            return CreateSnapshot(primaryKeys, parameters.Count);
        }

        public async Task<Snapshot> History(HistoryParameters parameters)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>();
            if (parameters.Since != null)
                clauses.Add(Builders<BsonDocument>.Filter.Gt(Field.WHEN, BsonDateTime.Create(parameters.Since)));

            var primaryKeys = await FetchPrimaryKeys(clauses).ConfigureAwait(false);
            return CreateSnapshot(primaryKeys, parameters.Count);
        }

        public async Task<IList<string>> FetchPrimaryKeys(FilterDefinition<BsonDocument> query)
        {
            var result = await collection.FindAsync(query, new FindOptions<BsonDocument> { Sort = Builders<BsonDocument>.Sort.Descending(Field.WHEN), Projection = Builders<BsonDocument>.Projection.Include(Field.PRIMARYKEY) }).ConfigureAwait(false);
            return result.ToEnumerable()
             .Select(doc => doc.GetValue(Field.PRIMARYKEY).AsString).ToList();
        }

        private Task<IList<string>> FetchPrimaryKeys(IEnumerable<FilterDefinition<BsonDocument>> clauses)
        {
            FilterDefinition<BsonDocument> query = clauses.Any() ? Builders<BsonDocument>.Filter.And(clauses) : Builders<BsonDocument>.Filter.Empty;
            return FetchPrimaryKeys(query);
        }

        private Snapshot CreateSnapshot(IEnumerable<string> keys, int? count = null, IList<string> includes = null, IList<string> reverseIncludes = null)
        {
            Uri link = new Uri(RestOperation.HISTORY, UriKind.Relative);
            Snapshot snapshot = Snapshot.Create(Bundle.BundleType.History, link, keys, "history", count, includes, reverseIncludes);
            return snapshot;
        }

    }
}