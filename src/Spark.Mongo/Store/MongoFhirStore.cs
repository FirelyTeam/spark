/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;

using MongoDB.Bson;
using MongoDB.Driver;
using MonQ = MongoDB.Driver.Builders;

using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;


namespace Spark.Store.Mongo
{

    public class MongoFhirStore : IFhirStore, IGenerator, ISnapshotStore 
    {
        MongoDatabase database;
        MongoCollection<BsonDocument> collection;

        public MongoFhirStore(MongoDatabase database)
        {
            this.database = database;
            this.collection = database.GetCollection(Collection.RESOURCE);
            //this.transaction = new MongoSimpleTransaction(collection);
        }

        public IList<string> List(string resource, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Field.TYPENAME, resource));
            if (since != null)
            {
                clauses.Add(MonQ.Query.GT(Field.WHEN, BsonDateTime.Create(since)));
            }
            clauses.Add(MonQ.Query.EQ(Field.STATE, Value.CURRENT));

            return FetchPrimaryKeys(clauses);
        }

        public IList<string> History(string resource, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Field.TYPENAME, resource));
            if (since != null)
                clauses.Add(MonQ.Query.GT(Field.WHEN, BsonDateTime.Create(since)));

            return FetchPrimaryKeys(clauses);
        }

        public IList<string> History(IKey key, DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Field.TYPENAME, key.TypeName));
            clauses.Add(MonQ.Query.EQ(Field.RESOURCEID, key.ResourceId));
            if (since != null)
                clauses.Add(MonQ.Query.GT(Field.WHEN, BsonDateTime.Create(since)));

            return FetchPrimaryKeys(clauses);
        }

        public IList<string> History(DateTimeOffset? since = null)
        {
            var clauses = new List<IMongoQuery>();
            if (since != null)
                clauses.Add(MonQ.Query.GT(Field.WHEN, BsonDateTime.Create(since)));

            return FetchPrimaryKeys(clauses);
        }

        public bool Exists(IKey key)
        {
            // PERF: efficiency
            Interaction existing = Get(key);
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

        public Interaction Get(IKey key)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Field.TYPENAME, key.TypeName));
            clauses.Add(MonQ.Query.EQ(Field.RESOURCEID, key.ResourceId));
            
            if (key.HasVersionId())
            {
                clauses.Add(MonQ.Query.EQ(Field.VERSIONID, key.VersionId));
            }
            else
            {
                clauses.Add(MonQ.Query.EQ(Field.STATE, Value.CURRENT));
            }

            IMongoQuery query = MonQ.Query.And(clauses);

            BsonDocument document = collection.FindOne(query);
            return document.ToInteraction();

        }
        
        public IList<Interaction> Get(IEnumerable<string> identifiers, string sortby)
        {
            var clauses = new List<IMongoQuery>();
            IEnumerable<BsonValue> ids = identifiers.Select(i => (BsonValue)i);

            clauses.Add(MonQ.Query.In(Field.PRIMARYKEY, ids));
            
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

            return cursor.ToInteractions().ToList();
        }

        private void Supercede(IKey key)
        {
            var pk = key.ToBsonReferenceKey();
            IMongoQuery query = MonQ.Query.And(
                MonQ.Query.EQ(Field.REFERENCE, pk), 
                MonQ.Query.EQ(Field.STATE, Value.CURRENT)
            );

            IMongoUpdate update = new UpdateDocument("$set",
            new BsonDocument
            {
                { Field.STATE, Value.SUPERCEDED },
            }
            );
            collection.Update(query, update);
        }

        private void Supercede(IEnumerable<IKey> keys)
        {
            var pks = keys.Select(k => k.ToBsonReferenceKey());
            IMongoQuery query = MonQ.Query.And(
                MonQ.Query.In(Field.REFERENCE, pks),
                MonQ.Query.EQ(Field.STATE, Value.CURRENT)
            );
            IMongoUpdate update = new UpdateDocument("$set",
            new BsonDocument
            {
                { Field.STATE, Value.SUPERCEDED },
            }
            );
            collection.Update(query, update);
        }

        
        public void Add(Interaction entry)
        {
            BsonDocument document = SparkBsonHelper.ToBsonDocument(entry);
            Supercede(entry.Key);
            collection.Save(document);
        }

        public void Add(IEnumerable<Interaction> interactions)
        {
            var keys = interactions.Select(i => i.Key);
            Supercede(keys);
            IList<BsonDocument> documents = interactions.Select(SparkBsonHelper.ToBsonDocument).ToList();
            collection.InsertBatch(documents);
        }
        
        public void Replace(Interaction entry)
        {
            string versionid = entry.Resource.Meta.VersionId;
            
            IMongoQuery query = MonQ.Query.EQ(Field.VERSIONID, versionid);
            BsonDocument current = collection.FindOne(query);
            BsonDocument replacement = SparkBsonHelper.ToBsonDocument(entry);
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

        public IList<string> FetchPrimaryKeys(IMongoQuery query)
        {
            MongoCursor<BsonDocument> cursor = collection.Find(query);
            cursor = cursor.SetFields(MonQ.Fields.Include(Field.PRIMARYKEY));

            return cursor.Select(doc => doc.GetValue(Field.PRIMARYKEY).AsString).ToList();

        }

        public IList<string> FetchPrimaryKeys(IEnumerable<IMongoQuery> clauses)
        {
            IMongoQuery query = MonQ.Query.And(clauses);
            return FetchPrimaryKeys(query);
        }

    }


    


}
