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
    // todo: DSTU2 add ITagStore
    public class MongoFhirStore : IFhirStore, IGenerator // ITagStore, 
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

        public IEnumerable<string> List(string resource, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Collection.RESOURCE, resource));
            if (since != null)
                clauses.Add(MonQ.Query.GT(Field.VERSIONDATE, BsonDateTime.Create(since)));
            clauses.Add(MonQ.Query.EQ(Field.STATE, Value.CURRENT));
            
            // todo: DSTU2
            //clauses.Add(MonQ.Query.NE(Field.ENTRYTYPE, typeof(DeletedEntry).Name));

            return FetchKeys(clauses);
        }

        public IEnumerable<string> History(string resource, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Collection.RESOURCE, resource));
            if (since != null)
                clauses.Add(MonQ.Query.GT(Field.VERSIONDATE, BsonDateTime.Create(since)));

            return FetchKeys(clauses);
        }

        public IEnumerable<string> History(Key key, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Field.OPERATION, key.TypeName));
            clauses.Add(MonQ.Query.EQ(Field.RESOURCEID, key.ResourceId));
            if (since != null)
                clauses.Add(MonQ.Query.GT(Field.VERSIONDATE, BsonDateTime.Create(since)));

            return FetchKeys(clauses);
        }

        public IEnumerable<string> History(DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();
            if (since != null)
                clauses.Add(MonQ.Query.GT(Field.VERSIONDATE, BsonDateTime.Create(since)));

            return FetchKeys(clauses);
        }

        public bool Exists(Key key)
        {
            // todo: efficiency
            Entry existing = Get(key);
            return (existing != null);
        }

        public Entry Get(string recordid)
        {
            IMongoQuery query = MonQ.Query.EQ(Field.RECORDID, recordid);
            BsonDocument document = collection.FindOne(query);
            if (document != null)
            {
                Entry entry = BsonToEntry(document);
                return entry;
            }
            else
            {
                return null;
            }
        }

        public Entry Get(Key key)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Field.TYPENAME, key.TypeName));
            // TODO: BRIAN - Not sure where the set for the current takes place...
            // clauses.Add(MonQ.Query.EQ(Field.STATE, Value.CURRENT));
            clauses.Add(MonQ.Query.EQ(Field.RESOURCEID, key.ResourceId));
            
            if (key.HasVersion)
            {
                clauses.Add(MonQ.Query.EQ(Field.VERSIONID, key.ToString()));
            }

            IMongoQuery query = MonQ.Query.And(clauses);

            BsonDocument document = collection.FindOne(query);
            if (document != null)
            {
                Entry entry = BsonToEntry(document);
                return entry;
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<Entry> Get(IEnumerable<string> identifiers, string sortby)
        {
            var clauses = new List<IMongoQuery>();
            IEnumerable<BsonValue> ids = identifiers.Select(i => (BsonValue)i);

            clauses.Add(MonQ.Query.In(Field.RECORDID, ids));
                clauses.Add(MonQ.Query.EQ(Field.STATE, Value.CURRENT));
            

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
                Entry entry = BsonToEntry(document);
                yield return entry;
            }
        }

        public void Add(Entry entry)
        {
            BsonDocument document = EntryToBson(entry);
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
            List<BsonDocument> documents = entries.Select(EntryToBson).ToList();
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
            string key = entry.Resource.Meta.VersionId;
            
            IMongoQuery query = MonQ.Query.EQ(Field.VERSIONID, key);
            BsonDocument current = collection.FindOne(query);
            BsonDocument replacement = EntryToBson(entry);
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

            string value = Field.KEYPREFIX + document[Field.COUNTERVALUE].AsInt32.ToString();
            return value;
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
            collection.CreateIndex(Field.STATE, Field.OPERATION, Field.TYPENAME);
            collection.CreateIndex(Field.RECORDID, Field.STATE);
            var index = MonQ.IndexKeys.Descending(Field.VERSIONDATE).Ascending(Field.TYPENAME);
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

        public IEnumerable<string> FetchKeys(IMongoQuery query)
        {
            MongoCursor<BsonDocument> cursor = collection.Find(query);
            cursor = cursor.SetFields(MonQ.Fields.Include(Field.VERSIONID));

            return cursor.Select(doc => doc.GetValue(Field.RECORDID).AsString);
        }

        public IEnumerable<string> FetchKeys(IEnumerable<IMongoQuery> clauses)
        {
            IMongoQuery query = MonQ.Query.And(clauses);
            return FetchKeys(query);
        }

        

        private static BsonDocument EntryToBson(Entry entry)
        {
            // todo: HACK!
            Hack.MongoPeriod(entry);
            
            string json = FhirSerializer.SerializeResourceToJson(entry.Resource);
            // todo: DSTU2 - this does not work anymore for deletes!!!

            BsonDocument document = BsonDocument.Parse(json);
            AddMetaData(document, entry.Resource);
            return document;
        }

        private static Entry BsonToEntry(BsonDocument document)
        {
            try
            {
                DateTime stamp = GetVersionDate(document);
                RemoveMetadata(document);
                string json = document.ToJson();
                Resource resource = FhirParser.ParseResourceFromJson(json);
                Entry entry = new Entry(resource);
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

        private static void AddVersionDate(Entry entry, DateTime stamp)
        {
            // todo: DSTU2
            /*
            if (resource is Resource)
            {
                (resource as ResourceEntry).LastUpdated = stamp;
            }
            if (resource is DeletedEntry)
            {
                (resource as DeletedEntry).When = stamp;
            }
            */
            entry.When = stamp;
        }

        private static void RemoveMetadata(BsonDocument document)
        {
            document.Remove(Field.RECORDID);
            document.Remove(Field.VERSIONDATE);
            document.Remove(Field.STATE);
            document.Remove(Field.VERSIONID);
            document.Remove(Field.TYPENAME);
            document.Remove(Field.OPERATION);
            document.Remove(Field.TRANSACTION);
        }

        private static void AddMetaData(BsonDocument document, Resource resource)
        {
            document[Field.VERSIONID] = resource.Meta.VersionId;
            document[Field.OPERATION] = resource.TypeName;
            document[Field.TYPENAME] = resource.TypeName;
            document[Field.VERSIONDATE] = GetVersionDate(resource) ?? DateTime.UtcNow; 
        }

        private static void TransferMetadata(BsonDocument from, BsonDocument to)
        {
            to[Field.STATE] = from[Field.STATE];
            
            to[Field.VERSIONID] = from[Field.VERSIONID];
            to[Field.VERSIONDATE] = from[Field.VERSIONDATE];
            
            to[Field.OPERATION] = from[Field.OPERATION];
            to[Field.TYPENAME] = from[Field.TYPENAME];
        }

        private static DateTime? GetVersionDate(Resource resource)
        {
            DateTimeOffset? result = resource.Meta.LastUpdated;
                // todo: DSTU2
                /*(resource is ResourceEntry)
                ? ((ResourceEntry)resource).LastUpdated
                : ((DeletedEntry)resource).When;
                */

            // todo: moet een ontbrekende version date niet in de service gevuld worden?
            //return (result != null) ? result.Value.UtcDateTime : null;
            return (result != null) ? result.Value.UtcDateTime : (DateTime?)null;
        }



    }



}
