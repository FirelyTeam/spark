using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Config;
using Spark.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonQ = MongoDB.Driver.Builders;

namespace Spark.Core
{

    public interface IFhirStorage
    {
        // Keys
        IEnumerable<Uri> List(string resource, DateTimeOffset? since = null);
        IEnumerable<Uri> History(string resource, DateTimeOffset? since = null);
        IEnumerable<Uri> History(Uri key, DateTimeOffset? since = null);
        IEnumerable<Uri> History(DateTimeOffset? since = null);

        // BundleEntries
        BundleEntry Get(Uri key);
        IEnumerable<BundleEntry> Get(IEnumerable<Uri> keys);

        void Add(BundleEntry entry);
        void Add(IEnumerable<BundleEntry> entries);

        void Replace(BundleEntry entry);

        // Snapshots
        void AddSnapshot(Snapshot snapshot);
        Snapshot GetSnapshot(string key);
    }

    public interface IGenerator
    {
        string NextKey(Resource resource);
        string NextHistoryKey(Resource resource);
    }

    public interface ITagStore
    {
        IEnumerable<Tag> Tags();
        IEnumerable<Tag> Tags(string collection);
        IEnumerable<Uri> Find(params Tag[] tags);
    }

    // todo: move the following functionality to teh service layer
    // - read: DateTime to DateTimeOffset, write: DateTimeOffset to DateTime (also for versiondate (When, LastUpdated))
    // - exclude Query resource

    public class NewMongoFhirStore : IFhirStorage, IGenerator
    {
        MongoDatabase database;
        MongoCollection<BsonDocument> collection;
        MongoTransaction transaction;

        public NewMongoFhirStore(MongoDatabase database)
        {
            this.database = database;
            this.collection = database.GetCollection(Collection.RESOURCE);
            this.transaction = new MongoTransaction(collection);
        }

        public IEnumerable<Uri> List(string resource, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Collection.RESOURCE, resource));
            if (since != null)   
                clauses.Add(MonQ.Query.GT(Field.VERSIONDATE, BsonDateTime.Create(since)));
            clauses.Add(MonQ.Query.EQ(Field.STATE, Value.CURRENT));
            clauses.Add(MonQ.Query.NE(Field.ENTRYTYPE, typeof(DeletedEntry).Name));

            return FetchKeys(clauses);
        }

        public IEnumerable<Uri> History(string resource, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Collection.RESOURCE, resource));
            if (since != null)
                clauses.Add(MonQ.Query.GT(Field.VERSIONDATE, BsonDateTime.Create(since)));

            return FetchKeys(clauses);
        }

        public IEnumerable<Uri> History(Uri key, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Field.ID, key.ToString()));
            if (since != null) 
                clauses.Add(MonQ.Query.GT(Field.VERSIONDATE, BsonDateTime.Create(since)));

            return FetchKeys(clauses);
        }

        public IEnumerable<Uri> History(DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();
            if (since != null) 
                clauses.Add(MonQ.Query.GT(Field.VERSIONDATE, BsonDateTime.Create(since)));

            return FetchKeys(clauses);
        }

        public BundleEntry Get(Uri key)
        {
            string field = AnalyseKey(key);
            
            IMongoQuery query =
                MonQ.Query.And(
                    MonQ.Query.EQ(field, key.ToString()),
                    MonQ.Query.EQ(Field.STATE, Value.CURRENT)
                );

            BsonDocument document = collection.FindOne(query);
            if (document != null)
            {
                BundleEntry entry = BsonToBundleEntry(document);
                return entry;
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<BundleEntry> Get(IEnumerable<Uri> keys)
        {
            string field = AnalyseKey(keys.First());

            IEnumerable<BsonValue> ids = keys.Select(k => (BsonValue)k.ToString());
            
            AnalyseKey(keys.First());
            IMongoQuery query =
                MonQ.Query.And(
                    MonQ.Query.In(field, ids),
                    MonQ.Query.EQ(Field.STATE, Value.CURRENT)
                );

            MongoCursor<BsonDocument> cursor = collection.Find(query);

            foreach (BsonDocument document in cursor)
            {
                BundleEntry entry = BsonToBundleEntry(document);
                yield return entry;
            }
            
        }

        public void Add(BundleEntry entry)
        {
            BsonDocument document = BundleEntryToBson(entry);
            try
            {
                transaction.Begin();
                transaction.Insert(document);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void Add(IEnumerable<BundleEntry> entries)
        {
            List<BsonDocument> documents = entries.Select(BundleEntryToBson).ToList();
            try
            {
                transaction.Begin();
                transaction.InsertBatch(documents);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void Replace(BundleEntry entry)
        {
            BsonDocument document = BundleEntryToBson(entry);
            document[Field.STATE] = Value.CURRENT;
            document[Field.BATCHID] = Guid.NewGuid();

            //FindAndModifyArgs args = new FindAndModifyArgs();
            IMongoQuery query = MonQ.Query.EQ(Field.VERSIONID, entry.SelfLink.ToString());
            IMongoUpdate update = MonQ.Update.Replace(document);
            collection.Update(query, update);
        }

        public void AddSnapshot(Snapshot snapshot)
        {
            var collection = database.GetCollection(Collection.SNAPSHOT);
            collection.Save<Snapshot>(snapshot);
        }

        public Snapshot GetSnapshot(string key)
        {
            var collection = database.GetCollection(Collection.SNAPSHOT);
            return collection.FindOneByIdAs<Snapshot>(key);
        }

        public string NextKey(string name)
        {
            var collection = database.GetCollection(Collection.COUNTERS);

            FindAndModifyArgs args = new FindAndModifyArgs();
            args.Query = MonQ.Query.EQ("_id", name);
            args.Update = MonQ.Update.Inc(Field.COUNTERVALUE, 1);
            args.Fields = MonQ.Fields.Include(Field.COUNTERVALUE);
            FindAndModifyResult result = collection.FindAndModify(args);
            BsonDocument document = result.ModifiedDocument;

            string value = document[Field.COUNTERVALUE].AsString;
            return value;
        }

        public string NextKey(Resource resource)
        {
            string name = resource.GetType().Name;
            return NextKey(name);
        }

        public string NextHistoryKey(Resource resource)
        {
            string name = resource.GetType().Name + "_history";
            return NextKey(name);

        }

        public static class Collection
        {
            public const string RESOURCE = "resources";
            public const string COUNTERS = "counters";
            public const string SNAPSHOT = "snapshots";
        }

        public static class Field
        {
            public const string STATE = "@state";
            public const string VERSIONDATE = "@versionDate";
            public const string ENTRYTYPE = "@entryType";
            public const string COLLECTION = "@collection";
            public const string BATCHID = "@batchId";

            public const string ID = "id";
            public const string VERSIONID = "_id";
            //public const string RECORDID = "_id"; // SelfLink is re-used as the Mongo key
            public const string COUNTERVALUE = "last";
        }

        public static class Value
        {
            public const string CURRENT = "current";
            public const string SUPERCEDED = "superceded";
        }

        public string AnalyseKey(Uri key)
        {
            bool history = key.ToString().Contains(RestOperation.HISTORY);
            return (history) ? Field.VERSIONID : Field.ID;
        }

        public IEnumerable<Uri> FetchKeys(IMongoQuery query)
        {
            MongoCursor<BsonDocument> cursor = collection
                .Find(query)
                .SetSortOrder(MonQ.SortBy.Descending(Field.VERSIONDATE))
                .SetFields(MonQ.Fields.Include(Field.VERSIONID));

            return cursor.Select(doc => new Uri(doc.GetValue(Field.VERSIONID).AsString));
        }

        public IEnumerable<Uri> FetchKeys(IEnumerable<IMongoQuery> clauses)
        {
            IMongoQuery query = MonQ.Query.And(clauses);
            return FetchKeys(query);
        }

        private static BsonDocument BundleEntryToBson(BundleEntry entry)
        {
            string json = FhirSerializer.SerializeBundleEntryToJson(entry);
            BsonDocument document = BsonDocument.Parse(json);
            AddMetaData(document, entry);
            return document;
        }
        
        private static BundleEntry BsonToBundleEntry(BsonDocument document)
        {
            try
            {
                // Remove our storage metadata before deserializing
                RemoveMetadata(document);
                string json = document.ToJson();
                BundleEntry entry = FhirParser.ParseBundleEntryFromJson(json);
                return entry;
            }
            catch (Exception inner)
            {
                throw new InvalidOperationException("Cannot parse MongoDb's json into a feed entry: ", inner);
            }

        }

        private static void RemoveMetadata(BsonDocument document)
        {
            document.Remove(Field.VERSIONDATE);
            document.Remove(Field.STATE);
            document.Remove(Field.VERSIONID);
            document.Remove(Field.ENTRYTYPE);
            document.Remove(Field.COLLECTION);
            document.Remove(Field.BATCHID);
        }

        private static void AddMetaData(BsonDocument document, BundleEntry entry)
        {
            document[Field.VERSIONID] = entry.Links.SelfLink.ToString();
            document[Field.ENTRYTYPE] = entry.GetType().Name; 
            document[Field.COLLECTION] = new ResourceIdentity(entry.Id).Collection;
            document[Field.VERSIONDATE] = VersionDateOf(entry).Value.UtcDateTime;
        }

        private static DateTimeOffset? VersionDateOf(BundleEntry entry)
        {
            if (entry is ResourceEntry)
            {
                return ((ResourceEntry)entry).LastUpdated;
            }
            else
            {
                return ((DeletedEntry)entry).When;
            }
        }

    }



}
