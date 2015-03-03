/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;
using MonQ = MongoDB.Driver.Builders;

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;

using Spark.Core;


namespace Spark.Store
{
    // DSTU2: tags
    // add tag store
    public class MongoFhirStore : IFhirStore, IGenerator // ITagStore, 
    {
        MongoDatabase database;
        MongoCollection<BsonDocument> collection;
        MongoTransaction transaction;

        public MongoFhirStore(MongoDatabase database)
        {
            this.database = database;
            this.collection = database.GetCollection(Collection.RESOURCE);
            this.transaction = new MongoTransaction(collection);
        }

        public IEnumerable<string> List(string resource, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Collection.RESOURCE, resource));
            if (since != null)
                clauses.Add(MonQ.Query.GT(Field.WHEN, BsonDateTime.Create(since)));
            clauses.Add(MonQ.Query.EQ(Field.STATE, Value.CURRENT));

            return FetchPrimaryKeys(clauses);
        }

        public IEnumerable<string> History(string resource, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Collection.RESOURCE, resource));
            if (since != null)
                clauses.Add(MonQ.Query.GT(Field.WHEN, BsonDateTime.Create(since)));

            return FetchPrimaryKeys(clauses);
        }

        public IEnumerable<string> History(IKey key, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Field.PRESENSE, key.TypeName));
            clauses.Add(MonQ.Query.EQ(Field.RESOURCEID, key.ResourceId));
            if (since != null)
                clauses.Add(MonQ.Query.GT(Field.WHEN, BsonDateTime.Create(since)));

            return FetchPrimaryKeys(clauses);
        }

        public IEnumerable<string> History(DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();
            if (since != null)
                clauses.Add(MonQ.Query.GT(Field.WHEN, BsonDateTime.Create(since)));

            return FetchPrimaryKeys(clauses);
        }

        public bool Exists(IKey key)
        {
            // todo: efficiency
            Entry existing = Get(key);
            return (existing != null);
        }

        public Entry Get(string primarykey)
        {
            IMongoQuery query = MonQ.Query.EQ(Field.PRIMARYKEY, primarykey);
            BsonDocument document = collection.FindOne(query);
            if (document != null)
            {
                Entry entry = SparkBsonHelper.BsonToEntry(document);
                return entry;
            }
            else
            {
                return null;
            }
        }

        public Entry Get(IKey key)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Field.TYPENAME, key.TypeName));
            clauses.Add(MonQ.Query.EQ(Field.RESOURCEID, key.ResourceId));
            
            if (key.HasVersionId)
            {
                clauses.Add(MonQ.Query.EQ(Field.VERSIONID, key.VersionId));
            }
            else
            {
                clauses.Add(MonQ.Query.EQ(Field.STATE, Value.CURRENT));
            }

            IMongoQuery query = MonQ.Query.And(clauses);

            BsonDocument document = collection.FindOne(query);
            return SparkBsonHelper.BsonToEntry(document);

        }

        public IEnumerable<Entry> Get(IEnumerable<string> identifiers, string sortby)
        {
            var clauses = new List<IMongoQuery>();
            IEnumerable<BsonValue> ids = identifiers.Select(i => (BsonValue)i);

            clauses.Add(MonQ.Query.In(Field.PRIMARYKEY, ids));
                clauses.Add(MonQ.Query.EQ(Field.STATE, Value.CURRENT));
            

            IMongoQuery query = MonQ.Query.And(clauses);
            MongoCursor<BsonDocument> cursor = collection.Find(query);

            if (sortby != null)
            {
                cursor = cursor.SetSortOrder(MonQ.SortBy.Ascending(sortby));
            }
            else
            {
                cursor = cursor.SetSortOrder(MonQ.SortBy.Descending(Field.WHEN));
            }

            foreach (BsonDocument document in cursor)
            {
                Entry entry = SparkBsonHelper.BsonToEntry(document);
                yield return entry;
            }
        }

        public void Add(Entry entry)
        {
            BsonDocument document = SparkBsonHelper.EntryToBson(entry);
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

        public void Add(IEnumerable<Entry> entries)
        {
            List<BsonDocument> documents = entries.Select(SparkBsonHelper.EntryToBson).ToList();
            foreach(var document in documents)
            {
                // TODO: BRIAN - Should be doing more than this here
                try
                {
                    this.collection.Save(document);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                }
            }
            
            // DSTU2: mongo store
            /*
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
            */
        }
        
        public void Replace(Entry entry)
        {
            string versionid = entry.Resource.Meta.VersionId;
            
            IMongoQuery query = MonQ.Query.EQ(Field.VERSIONID, versionid);
            BsonDocument current = collection.FindOne(query);
            BsonDocument replacement = SparkBsonHelper.EntryToBson(entry);
            SparkBsonHelper.TransferMetadata(current, replacement);

            IMongoUpdate update = MonQ.Update.Replace(replacement);
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

        public string Next(string name)
        {
            var collection = database.GetCollection(Collection.COUNTERS);

            FindAndModifyArgs args = new FindAndModifyArgs();
            args.Query = MonQ.Query.EQ("_id", name);
            args.Update = MonQ.Update.Inc(Field.COUNTERVALUE, 1);
            args.Fields = MonQ.Fields.Include(Field.COUNTERVALUE);
            args.Upsert = true;
            args.VersionReturned = FindAndModifyDocumentVersion.Modified;

            FindAndModifyResult result = collection.FindAndModify(args);
            BsonDocument document = result.ModifiedDocument;

            string value = document[Field.COUNTERVALUE].AsInt32.ToString();
            return value;
        }

        public static class Format
        {
            public static string RESOURCEID = "spark{0}";
            public static string VERSIONID = "spark{0}";
        }

        string IGenerator.NextResourceId(string resource)
        {
            string id = this.Next(resource);
            return string.Format(Format.RESOURCEID, id);
        }

        string IGenerator.NextVersionId(string resource)
        {
            string name = resource + "_history";
            string id = this.Next(name);
            return string.Format(Format.VERSIONID, id);
        }

        public bool KeyAllowed(string value)
        {
            if (value.StartsWith(Field.KEYPREFIX))
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


        private void dropCollections(IEnumerable<string> collections)
        {
            foreach (var name in collections)
            {
                database.DropCollection(name);
            }
        }
        
        // Drops all collections, including the special 'counters' collection for generating ids,
        // AND the binaries stored at Amazon S3
        private void EraseData()
        {
            // Don't try this at home
            var collectionsToDrop = new string[] { Collection.RESOURCE, Collection.COUNTERS, Collection.SNAPSHOT };
            dropCollections(collectionsToDrop);
            
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
            collection.CreateIndex(Field.STATE, Field.PRESENSE, Field.TYPENAME);
            collection.CreateIndex(Field.PRIMARYKEY, Field.STATE);
            var index = MonQ.IndexKeys.Descending(Field.WHEN).Ascending(Field.TYPENAME);
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

        public static class Collection
        {
            public const string RESOURCE = "resources";
            public const string COUNTERS = "counters";
            public const string SNAPSHOT = "snapshots";
        }

        public IEnumerable<string> FetchRecordPrimaryKeys(IMongoQuery query)
        {
            MongoCursor<BsonDocument> cursor = collection.Find(query);
            cursor = cursor.SetFields(MonQ.Fields.Include(Field.PRIMARYKEY));

            return cursor.Select(doc => doc.GetValue(Field.PRIMARYKEY).AsString);
        }

        public IEnumerable<string> FetchPrimaryKeys(IEnumerable<IMongoQuery> clauses)
        {
            IMongoQuery query = MonQ.Query.And(clauses);
            return FetchRecordPrimaryKeys(query);
        }

    }


    


}
