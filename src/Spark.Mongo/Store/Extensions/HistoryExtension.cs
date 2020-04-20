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
    public class HistoryStore : IHistoryStore
    {
        IMongoDatabase database;
        IMongoCollection<BsonDocument> collection;
        public HistoryStore(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            this.collection = database.GetCollection<BsonDocument>(Collection.RESOURCE);
        }
   
        public Snapshot History(string resource, HistoryParameters parameters)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>();

            clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, resource));
            if (parameters.Since != null)
                clauses.Add(Builders<BsonDocument>.Filter.Gt(Field.WHEN, BsonDateTime.Create(parameters.Since)));

            return CreateSnapshot(FetchPrimaryKeys(clauses), parameters.Count);
        }

        public Snapshot History(IKey key, HistoryParameters parameters)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>();

            clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, key.TypeName));
            clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.RESOURCEID, key.ResourceId));
            if (parameters.Since != null)
                clauses.Add(Builders<BsonDocument>.Filter.Gt(Field.WHEN, BsonDateTime.Create(parameters.Since)));

            return CreateSnapshot(FetchPrimaryKeys(clauses), parameters.Count);
        }

        public Snapshot History(HistoryParameters parameters)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>();
            if (parameters.Since != null)
                clauses.Add(Builders<BsonDocument>.Filter.Gt(Field.WHEN, BsonDateTime.Create(parameters.Since)));

            return CreateSnapshot(FetchPrimaryKeys(clauses), parameters.Count);
        }

        public IList<string> FetchPrimaryKeys(FilterDefinition<BsonDocument> query)
        {
            return collection.Find(query)
                .Sort(Builders<BsonDocument>.Sort.Descending(Field.WHEN))
                .Project(Builders<BsonDocument>.Projection.Include(Field.PRIMARYKEY))
                .ToEnumerable()
                .Select(doc => doc.GetValue(Field.PRIMARYKEY).AsString).ToList();
        }

        public IList<string> FetchPrimaryKeys(IEnumerable<FilterDefinition<BsonDocument>> clauses)
        {
            FilterDefinition<BsonDocument> query = clauses.Any() ? Builders<BsonDocument>.Filter.And(clauses) : Builders<BsonDocument>.Filter.Empty;
            return FetchPrimaryKeys(query);
        }

        private Snapshot CreateSnapshot(IEnumerable<string> keys, int? count = null, IList<string> includes = null, IList<string> reverseIncludes = null)
        {
            Uri link =  new Uri(RestOperation.HISTORY, UriKind.Relative);
            Snapshot snapshot = Snapshot.Create(Bundle.BundleType.History, link, keys, "history" , count, includes, reverseIncludes);
            return snapshot;
        }
     
    }
}