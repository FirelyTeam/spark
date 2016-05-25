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
        MongoDatabase database;
        MongoCollection<BsonDocument> collection;

        public MongoFhirStoreOther(string mongoUrl, IFhirStore mongoFhirStoreOther)
        {
            _mongoFhirStoreOther = mongoFhirStoreOther;
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            this.collection = database.GetCollection(Collection.RESOURCE);
            //this.transaction = new MongoSimpleTransaction(collection);
        }

        //TODO: I've commented this. Do we still need it?
        //public IList<string> List(string resource, DateTimeOffset? since = null)
        //{
        //    var clauses = new List<IMongoQuery>();

        //    clauses.Add(MongoDB.Driver.Builders.Query.EQ(Field.TYPENAME, resource));
        //    if (since != null)
        //    {
        //        clauses.Add(MongoDB.Driver.Builders.Query.GT(Field.WHEN, BsonDateTime.Create(since)));
        //    }
        //    clauses.Add(MongoDB.Driver.Builders.Query.EQ(Field.STATE, Value.CURRENT));

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
        //    IMongoQuery query = MonQ.Query.EQ(Field.PRIMARYKEY, primarykey);
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
            var clauses = new List<IMongoQuery>();
            IEnumerable<BsonValue> ids = identifiers.Select(i => (BsonValue)i);

            clauses.Add(MongoDB.Driver.Builders.Query.In(Field.REFERENCE, ids));
            clauses.Add(MongoDB.Driver.Builders.Query.EQ(Field.STATE, Value.CURRENT));
            IMongoQuery query = MongoDB.Driver.Builders.Query.And(clauses);

            MongoCursor<BsonDocument> cursor = collection.Find(query);

            if (sortby != null)
            {
                cursor = cursor.SetSortOrder(MongoDB.Driver.Builders.SortBy.Ascending(sortby));
            }
            else
            {
                cursor = cursor.SetSortOrder(MongoDB.Driver.Builders.SortBy.Descending(Field.WHEN));
            }

            return cursor.ToEntries().ToList();
        }

        private void Supercede(IEnumerable<IKey> keys)
        {
            var pks = keys.Select(k => k.ToBsonReferenceKey());
            IMongoQuery query = MongoDB.Driver.Builders.Query.And(
                MongoDB.Driver.Builders.Query.In(Field.REFERENCE, pks),
                MongoDB.Driver.Builders.Query.EQ(Field.STATE, Value.CURRENT)
                );
            IMongoUpdate update = new UpdateDocument("$set",
                new BsonDocument
                {
                    { Field.STATE, Value.SUPERCEDED },
                }
                );
            collection.Update(query, update);
        }

        public void Add(IEnumerable<Entry> entries)
        {
            var keys = entries.Select(i => i.Key);
            Supercede(keys);
            IList<BsonDocument> documents = entries.Select(SparkBsonHelper.ToBsonDocument).ToList();
            collection.InsertBatch(documents);
        }

        public void Replace(Entry entry)
        {
            string versionid = entry.Resource.Meta.VersionId;

            IMongoQuery query = MongoDB.Driver.Builders.Query.EQ(Field.VERSIONID, versionid);
            BsonDocument current = collection.FindOne(query);
            BsonDocument replacement = SparkBsonHelper.ToBsonDocument(entry);
            SparkBsonHelper.TransferMetadata(current, replacement);

            IMongoUpdate update = MongoDB.Driver.Builders.Update.Replace(replacement);
            collection.Update(query, update);
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
            IMongoQuery query = MonQ.Query.EQ(Field.COLLECTION, resourcetype);
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