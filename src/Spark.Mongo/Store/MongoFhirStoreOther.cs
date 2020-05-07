using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Store.Interfaces;

namespace Spark.Store.Mongo
{
    //TODO: decide if we still need this
    public class MongoFhirStoreOther
    {
        private readonly IFhirStore _mongoFhirStoreOther;
        IMongoDatabase database;
        IMongoCollection<BsonDocument> collection;

        public MongoFhirStoreOther(string mongoUrl, IFhirStore mongoFhirStoreOther)
        {
            _mongoFhirStoreOther = mongoFhirStoreOther;
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            this.collection = database.GetCollection<BsonDocument>(Collection.RESOURCE);
            //this.transaction = new MongoSimpleTransaction(collection);
        }

        //TODO: I've commented this. Do we still need it?
        //public IList<string> List(string resource, DateTimeOffset? since = null)
        //{
        //    var clauses = new List<FilterDefinition<BsonDocument>>();

        //    clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, resource));
        //    if (since != null)
        //    {
        //        clauses.Add(Builders<BsonDocument>.Filter.GT(Field.WHEN, BsonDateTime.Create(since)));
        //    }
        //    clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT));

        //    return FetchPrimaryKeys(clauses);
        //}
  

        public bool Exists(IKey key)
        {
            // PERF: efficiency
            Entry existing = _mongoFhirStoreOther.Get(key);
            return (existing != null);
        }

        //public Interaction Get(string primarykey)
        //{
        //    FilterDefinition<BsonDocument> query = MonQ.Query.Eq(Field.PRIMARYKEY, primarykey);
        //    BsonDocument document = collection.FindOne(query);
        //    if (document != null)
        //    {
        //        Interaction entry = document.ToInteraction();
        //        return entry;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        public IList<Entry> GetCurrent(IEnumerable<string> identifiers, string sortby = null)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>();
            IEnumerable<BsonValue> ids = identifiers.Select(i => (BsonValue)i);

            clauses.Add(Builders<BsonDocument>.Filter.In(Field.REFERENCE, ids));
            clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT));
            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.And(clauses);

            var cursor = collection.Find(query);

            if (sortby != null)
            {
                cursor = cursor.Sort(Builders<BsonDocument>.Sort.Ascending(sortby));
            }
            else
            {
                cursor = cursor.Sort(Builders<BsonDocument>.Sort.Descending(Field.WHEN));
            }

            return cursor.ToEnumerable().ToEntries().ToList();
        }

        private void Supercede(IEnumerable<IKey> keys)
        {
            var pks = keys.Select(k => k.ToBsonReferenceKey());
            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.In(Field.REFERENCE, pks),
                Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT)
                );
            UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set(Field.STATE, Value.SUPERCEDED);
            collection.UpdateMany(query, update);
        }

        public void Add(IEnumerable<Entry> entries)
        {
            var keys = entries.Select(i => i.Key);
            Supercede(keys);
            IList<BsonDocument> documents = entries.Select(SparkBsonHelper.ToBsonDocument).ToList();
            collection.InsertMany(documents);
        }

        public void Replace(Entry entry)
        {
            /*
            string versionid = entry.Resource.Meta.VersionId;

            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.Eq(Field.VERSIONID, versionid);
            BsonDocument current = collection.Find(query).FirstOrDefault();
            BsonDocument replacement = SparkBsonHelper.ToBsonDocument(entry);
            SparkBsonHelper.TransferMetadata(current, replacement);

            IMongoUpdate update = MongoDB.Driver.Builders.Update.Replace(replacement);
            collection.Update(query, update);
            */
        }

        public bool CustomResourceIdAllowed(string value)
        {
            if (value.StartsWith(Value.IDPREFIX))
            {
                string remainder = value.Substring(1);
                int i;
                bool isint = int.TryParse(remainder, out i);
                return !isint;
            }
            return true;
        }

        /*public Tag BsonValueToTag(BsonValue item)
        {
            Tag tag = new Tag(
                   item["term"].AsString,
                   new Uri(item["scheme"].AsString),
                   item["label"].AsString);

            return tag;
        }

        public IEnumerable<Tag> Tags()
        {
            return collection.Distinct(Field.CATEGORY).Select(BsonValueToTag);
        }

        public IEnumerable<Tag> Tags(string resourcetype)
        {
            FilterDefinition<BsonDocument> query = MonQ.Query.Eq(Field.COLLECTION, resourcetype);
            return collection.Distinct(Field.CATEGORY, query).Select(BsonValueToTag);
        }

        public IEnumerable<Uri> Find(params Tag[] tags)
        {
            throw new NotImplementedException("Finding tags is not implemented on database level");
        }
        */

     

    

        /// <summary>
        /// Does a complete wipe and reset of the database. USE WITH CAUTION!
        /// </summary>
     

       
    }
}