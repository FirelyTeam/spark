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
    public class HistoryExtension : IHistoryExtension
    {
        MongoDatabase database;
        MongoCollection<BsonDocument> collection;
        public HistoryExtension(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            this.collection = database.GetCollection(Collection.RESOURCE);
        }
        public void OnExtensionAdded(IFhirStore extensibleObject)
        {
        }

        public void OnEntryAdded(Entry entry)
        {
        }

        public Snapshot History(string resource, HistoryParameters parameters)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MongoDB.Driver.Builders.Query.EQ(Field.TYPENAME, resource));
            if (parameters.Since != null)
                clauses.Add(MongoDB.Driver.Builders.Query.GT(Field.WHEN, BsonDateTime.Create(parameters.Since)));

            return CreateSnapshot(FetchPrimaryKeys(clauses), parameters.SortBy, parameters.Count);
        }

        public Snapshot History(IKey key, HistoryParameters parameters)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MongoDB.Driver.Builders.Query.EQ(Field.TYPENAME, key.TypeName));
            clauses.Add(MongoDB.Driver.Builders.Query.EQ(Field.RESOURCEID, key.ResourceId));
            if (parameters.Since != null)
                clauses.Add(MongoDB.Driver.Builders.Query.GT(Field.WHEN, BsonDateTime.Create(parameters.Since)));

            return CreateSnapshot(FetchPrimaryKeys(clauses), parameters.SortBy, parameters.Count);
        }

        public Snapshot History(HistoryParameters parameters)
        {
            var clauses = new List<IMongoQuery>();
            if (parameters.Since != null)
                clauses.Add(MongoDB.Driver.Builders.Query.GT(Field.WHEN, BsonDateTime.Create(parameters.Since)));

            return CreateSnapshot(FetchPrimaryKeys(clauses), parameters.SortBy, parameters.Count);
        }

        public IList<string> FetchPrimaryKeys(IMongoQuery query)
        {
            MongoCursor<BsonDocument> cursor = collection.Find(query);
            cursor = cursor.SetFields(MongoDB.Driver.Builders.Fields.Include(Field.PRIMARYKEY));

            return cursor.Select(doc => doc.GetValue(Field.PRIMARYKEY).AsString).ToList();

        }

        public IList<string> FetchPrimaryKeys(IEnumerable<IMongoQuery> clauses)
        {
            IMongoQuery query = clauses.Any() ? MongoDB.Driver.Builders.Query.And(clauses) : MongoDB.Driver.Builders.Query.Empty;
            return FetchPrimaryKeys(query);
        }

       

        private Snapshot CreateSnapshot(IEnumerable<string> keys, string sortby = null, int? count = null, IList<string> includes = null, IList<string> reverseIncludes = null)
        {
            Uri link =  new Uri(RestOperation.HISTORY, UriKind.Relative);
            Snapshot snapshot = Snapshot.Create(Bundle.BundleType.History, link, keys, sortby, count, includes, reverseIncludes);
            return snapshot;
        }


     
    }
}