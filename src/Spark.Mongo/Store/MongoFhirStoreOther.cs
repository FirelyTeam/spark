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
    public class MongoFhirStoreOther :  IFhirStoreAdministration
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
        public IList<string> List(string resource, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MongoDB.Driver.Builders.Query.EQ(Field.TYPENAME, resource));
            if (since != null)
            {
                clauses.Add(MongoDB.Driver.Builders.Query.GT(Field.WHEN, BsonDateTime.Create(since)));
            }
            clauses.Add(MongoDB.Driver.Builders.Query.EQ(Field.STATE, Value.CURRENT));

            return FetchPrimaryKeys(clauses);
        }

        public IList<string> History(string resource, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MongoDB.Driver.Builders.Query.EQ(Field.TYPENAME, resource));
            if (since != null)
                clauses.Add(MongoDB.Driver.Builders.Query.GT(Field.WHEN, BsonDateTime.Create(since)));

            return FetchPrimaryKeys(clauses);
        }

        public IList<string> History(IKey key, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MongoDB.Driver.Builders.Query.EQ(Field.TYPENAME, key.TypeName));
            clauses.Add(MongoDB.Driver.Builders.Query.EQ(Field.RESOURCEID, key.ResourceId));
            if (since != null)
                clauses.Add(MongoDB.Driver.Builders.Query.GT(Field.WHEN, BsonDateTime.Create(since)));

            return FetchPrimaryKeys(clauses);
        }

        public IList<string> History(DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();
            if (since != null)
                clauses.Add(MongoDB.Driver.Builders.Query.GT(Field.WHEN, BsonDateTime.Create(since)));

            return FetchPrimaryKeys(clauses);
        }

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

        public static class Format
        {
            public static string RESOURCEID = "spark{0}";
            public static string VERSIONID = "spark{0}";
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

        private void TryDropCollection(string name)
        {
            try
            {
                database.DropCollection(name);
            }
            catch
            {
                //don't worry. if it's not there. it's not there.
            }
        }

        private void DropCollections(IEnumerable<string> collections)
        {
            foreach (var name in collections)
            {
                TryDropCollection(name);
            }
        }

        // Drops all collections, including the special 'counters' collection for generating ids,
        // AND the binaries stored at Amazon S3
        private void EraseData()
        {
            // Don't try this at home
            var collectionsToDrop = new string[] { Collection.RESOURCE, Collection.COUNTERS, Collection.SNAPSHOT };
            DropCollections(collectionsToDrop);

            /*
            // When using Amazon S3, remove blobs from there as well
            if (Config.Settings.UseS3)
            {
                using (var blobStorage = getBlobStorage())
                {
                    if (blobStorage != null)
                    {
                        blobStorage.Open();
                        blobStorage.DeleteAll();
                        blobStorage.Close();
                    }
                }
            }
            */
        }

        private void EnsureIndices()
        {
            collection.CreateIndex(Field.STATE, Field.METHOD, Field.TYPENAME);
            collection.CreateIndex(Field.PRIMARYKEY, Field.STATE);
            var index = MongoDB.Driver.Builders.IndexKeys.Descending(Field.WHEN).Ascending(Field.TYPENAME);
            collection.CreateIndex(index);
        }

        /// <summary>
        /// Does a complete wipe and reset of the database. USE WITH CAUTION!
        /// </summary>
        public void Clean()
        {
            EraseData();
            EnsureIndices();
        }

        public IList<string> FetchPrimaryKeys(IMongoQuery query)
        {
            MongoCursor<BsonDocument> cursor = collection.Find(query);
            cursor = cursor.SetFields(MongoDB.Driver.Builders.Fields.Include(Field.PRIMARYKEY));

            return cursor.Select(doc => doc.GetValue(Field.PRIMARYKEY).AsString).ToList();

        }

        public IList<string> FetchPrimaryKeys(IEnumerable<IMongoQuery> clauses)
        {
            IMongoQuery query = MongoDB.Driver.Builders.Query.And(clauses);
            return FetchPrimaryKeys(query);
        }
    }
}