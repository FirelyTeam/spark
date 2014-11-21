/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Config;
using Spark.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonQ = MongoDB.Driver.Builders;

namespace Spark.Store
{
    public class MongoFhirStore : IFhirStore, ITagStore, IGenerator
    {
        MongoDatabase database;
        MongoCollection<BsonDocument> collection;
        MongoTransaction transaction;

        public enum KeyType { Current, History };

        public MongoFhirStore(MongoDatabase database)
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

        public bool Exists(Uri key)
        {
            // todo: efficiency
            BundleEntry existing = Get(key);
            return (existing != null);
        }

        public BundleEntry Get(Uri key)
        {
            var clauses = new List<IMongoQuery>();

            if (AnalyseKey(key) == KeyType.History)
            {
                clauses.Add(MonQ.Query.EQ(Field.VERSIONID, key.ToString()));
            }
            else
            {
                clauses.Add(MonQ.Query.EQ(Field.ID, key.ToString()));
                clauses.Add(MonQ.Query.EQ(Field.STATE, Value.CURRENT));
            }

            IMongoQuery query = MonQ.Query.And(clauses);

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

        public IEnumerable<BundleEntry> Get(IEnumerable<Uri> keys, string sortby)
        {
            Uri firstkey = keys.FirstOrDefault();
            if (firstkey == null) yield break;

            var clauses = new List<IMongoQuery>();
            IEnumerable<BsonValue> ids = keys.Select(k => (BsonValue)k.ToString());

            if (AnalyseKey(firstkey) == KeyType.History)
            {
                clauses.Add(MonQ.Query.In(Field.VERSIONID, ids));
            }
            else
            {
                clauses.Add(MonQ.Query.In(Field.ID, ids));
                clauses.Add(MonQ.Query.EQ(Field.STATE, Value.CURRENT));
            }

            IMongoQuery query = MonQ.Query.And(clauses);
            MongoCursor<BsonDocument> cursor = collection.Find(query);

            if (sortby != null)
            {
                cursor = cursor.SetSortOrder(MonQ.SortBy.Ascending(sortby));
            }
            else
            {
                cursor = cursor.SetSortOrder(MonQ.SortBy.Descending(Field.VERSIONDATE));
            }

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
            string key = entry.SelfLink.ToString();
            
            IMongoQuery query = MonQ.Query.EQ(Field.VERSIONID, key);
            BsonDocument current = collection.FindOne(query);
            BsonDocument replacement = BundleEntryToBson(entry);
            TransferMetadata(current, replacement);

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

        public string NextKey(string name)
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

        public Tag BsonValueToTag(BsonValue item)
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

        // Drops all collections, including the special 'counters' collection for generating ids,
        // AND the binaries stored at Amazon S3
        private void EraseData()
        {
            // Don't try this at home
            var collectionsToDrop = new string[] { Collection.RESOURCE, Collection.COUNTERS, Collection.SNAPSHOT };

            foreach (var name in collectionsToDrop)
            {
                database.DropCollection(name);
            }

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
            collection.CreateIndex(Field.STATE, Field.ENTRYTYPE, Field.COLLECTION);
            collection.CreateIndex(Field.ID, Field.STATE);
            var index = MonQ.IndexKeys.Descending(Field.VERSIONDATE).Ascending(Field.COLLECTION);
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

        public static class Field
        {
            public const string STATE = "@state";
            public const string VERSIONDATE = "@versionDate";
            public const string ENTRYTYPE = "@entryType";
            public const string COLLECTION = "@collection";
            //public const string BATCHID = "@batchId";

            public const string ID = "id";
            public const string VERSIONID = "_id";
            //public const string RECORDID = "_id"; // SelfLink is re-used as the Mongo key
            public const string COUNTERVALUE = "last";
            public const string CATEGORY = "category";
        }

        public static class Value
        {
            public const string CURRENT = "current";
            public const string SUPERCEDED = "superceded";
        }

        public KeyType AnalyseKey(Uri key)
        {
            bool history = key.ToString().Contains(RestOperation.HISTORY);
            return (history) ? KeyType.History : KeyType.Current;
        }

        public KeyType AnalyseKeys(IEnumerable<Uri> keys)
        {
            Uri key = keys.FirstOrDefault();
            return (key != null) ? AnalyseKey(key) : KeyType.History; // doesn't matter which.
        }

        public IEnumerable<Uri> FetchKeys(IMongoQuery query)
        {
            MongoCursor<BsonDocument> cursor = collection.Find(query);
            cursor = cursor.SetFields(MonQ.Fields.Include(Field.VERSIONID));

            return cursor.Select(doc => doc.GetValue(Field.VERSIONID).AsString).Select(s => new Uri(s, UriKind.Relative));
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
                DateTime stamp = GetVersionDate(document);
                RemoveMetadata(document);
                string json = document.ToJson();
                BundleEntry entry = FhirParser.ParseBundleEntryFromJson(json);
                AddVersionDate(entry, stamp);
                return entry;
            }
            catch (Exception inner)
            {
                throw new InvalidOperationException("Cannot parse MongoDb's json into a feed entry: ", inner);
            }

        }

        private static DateTime GetVersionDate(BsonDocument document)
        {
            BsonValue value = document[Field.VERSIONDATE];
            return value.ToUniversalTime();
        }

        private static void AddVersionDate(BundleEntry entry, DateTime stamp)
        {
            if (entry is ResourceEntry)
            {
                (entry as ResourceEntry).LastUpdated = stamp;
            }
            if (entry is DeletedEntry)
            {
                (entry as DeletedEntry).When = stamp;
            }
        }

        private static void RemoveMetadata(BsonDocument document)
        {
            document.Remove(Field.VERSIONDATE);
            document.Remove(Field.STATE);
            document.Remove(Field.VERSIONID);
            document.Remove(Field.ENTRYTYPE);
            document.Remove(Field.COLLECTION);
        }

        private static void AddMetaData(BsonDocument document, BundleEntry entry)
        {
            document[Field.VERSIONID] = entry.Links.SelfLink.ToString();
            document[Field.ENTRYTYPE] = entry.TypeName();
             document[Field.COLLECTION] = entry.GetResourceTypeName();
            document[Field.VERSIONDATE] = GetVersionDate(entry) ?? DateTime.UtcNow; 
        }

        private static void TransferMetadata(BsonDocument from, BsonDocument to)
        {
            to[Field.STATE] = from[Field.STATE];
            
            to[Field.VERSIONID] = from[Field.VERSIONID];
            to[Field.VERSIONDATE] = from[Field.VERSIONDATE];
            
            to[Field.ENTRYTYPE] = from[Field.ENTRYTYPE];
            to[Field.COLLECTION] = from[Field.COLLECTION];
        }

        private static DateTime? GetVersionDate(BundleEntry entry)
        {
            DateTimeOffset? result = (entry is ResourceEntry)
                ? ((ResourceEntry)entry).LastUpdated
                : ((DeletedEntry)entry).When;

            // todo: moet een ontbrekende version date niet in de service gevuld worden?
            //return (result != null) ? result.Value.UtcDateTime : null;
            return (result != null) ? result.Value.UtcDateTime : (DateTime?)null;
        }

    }



}
