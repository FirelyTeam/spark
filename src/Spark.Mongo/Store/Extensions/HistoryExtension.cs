using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Auxiliary;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Store.Mongo;
using Task = System.Threading.Tasks.Task;

namespace Spark.Mongo.Store.Extensions
{
    public class HistoryStore : IHistoryStore
    {
        IMongoDatabase database;
        IMongoCollection<BsonDocument> collection;
        public HistoryStore(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            this.collection = database.GetCollection<BsonDocument>(Collection.RESOURCE);
        }

        [Obsolete("Use Async method version instead")]
        public Snapshot History(string typename, HistoryParameters parameters)
        {
            return Task.Run(() => HistoryAsync(typename, parameters)).GetAwaiter().GetResult();
        }

        [Obsolete("Use Async method version instead")]
        public Snapshot History(IKey key, HistoryParameters parameters)
        {
            return Task.Run(() => HistoryAsync(key, parameters)).GetAwaiter().GetResult();
        }

        [Obsolete("Use Async method version instead")]
        public Snapshot History(HistoryParameters parameters)
        {
            return Task.Run(() => HistoryAsync(parameters)).GetAwaiter().GetResult();
        }

        public async Task<Snapshot> HistoryAsync(string resource, HistoryParameters parameters)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>();

            clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, resource));
            if (parameters.Since != null)
                clauses.Add(Builders<BsonDocument>.Filter.Gt(Field.WHEN, BsonDateTime.Create(parameters.Since)));

            return CreateSnapshot(await FetchPrimaryKeysAsync(clauses).ConfigureAwait(false), parameters.Count);
        }

        public async Task<Snapshot> HistoryAsync(IKey key, HistoryParameters parameters)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>();

            clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, key.TypeName));
            clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.RESOURCEID, key.ResourceId));
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

            var cursor = await collection.FindAsync(query, new FindOptions<BsonDocument>
            {
                Sort = Builders<BsonDocument>.Sort.Descending(Field.WHEN),
                Projection = Builders<BsonDocument>.Projection.Include(Field.PRIMARYKEY)
            }).ConfigureAwait(false);

            return cursor.ToEnumerable().Select(doc => doc.GetValue(Field.PRIMARYKEY).AsString).ToList();
        }

        private static Snapshot CreateSnapshot(IEnumerable<string> keys, int? count = null, IList<string> includes = null, IList<string> reverseIncludes = null)
        {
            var link = new Uri(RestOperation.HISTORY, UriKind.Relative);
            var snapshot = Snapshot.Create(Bundle.BundleType.History, link, keys, "history", count, includes, reverseIncludes);
            return snapshot;
        }

    }
}