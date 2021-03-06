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
using Task = System.Threading.Tasks.Task;

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

        [Obsolete("Use HistoryAsync(string, HistoryParameters) instead")]
        public Snapshot History(string typename, HistoryParameters parameters)
        {
            return Task.Run(() => HistoryAsync(typename, parameters)).GetAwaiter().GetResult();
        }

        [Obsolete("Use HistoryAsync(IKey, HistoryParameters) instead")]
        public Snapshot History(IKey key, HistoryParameters parameters)
        {
            return Task.Run(() => HistoryAsync(key, parameters)).GetAwaiter().GetResult();
        }

        [Obsolete("Use HistoryAsync(HistoryParameters) instead")]
        public Snapshot History(HistoryParameters parameters)
        {
            return Task.Run(() => HistoryAsync(parameters)).GetAwaiter().GetResult();
        }

        public async Task<Snapshot> HistoryAsync(string resource, HistoryParameters parameters)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>
            {
                Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, resource)
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

        private async Task<IList<string>> FetchPrimaryKeysAsync(IList<FilterDefinition<BsonDocument>> clauses)
        {
            var query = clauses.Any()
                ? Builders<BsonDocument>.Filter.And(clauses)
                : Builders<BsonDocument>.Filter.Empty;

            var cursor = await _collection.FindAsync(query, new FindOptions<BsonDocument>
            {
                Sort = Builders<BsonDocument>.Sort.Descending(Field.WHEN),
                Projection = Builders<BsonDocument>.Projection.Include(Field.PRIMARYKEY)
            }).ConfigureAwait(false);

            return cursor.ToEnumerable().Select(doc => doc.GetValue(Field.PRIMARYKEY).AsString).ToList();
        }

        private static Snapshot CreateSnapshot(IEnumerable<string> keys, int? count = null, IList<string> includes = null, IList<string> reverseIncludes = null)
        {
            var link = new Uri(TransactionBuilder.HISTORY, UriKind.Relative);
            var snapshot = Snapshot.Create(Bundle.BundleType.History, link, keys, "history", count, includes, reverseIncludes);
            return snapshot;
        }
    }
}