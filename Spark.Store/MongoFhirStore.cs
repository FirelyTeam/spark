using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MongoDB.Driver;
using MonQ = MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using Spark.Data.AmazonS3;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
//using Spark.Service;
using Hl7.Fhir.Serialization;
//using Spark.Support;
using Spark.Core;
using Hl7.Fhir.Rest;

namespace Spark.Data.MongoDB
{

    public class MongoFhirStore : IFhirStore
    {
        MongoDatabase database; // todo: set

        //private const string BSON_BLOBID_MEMBER = "@blobId";
        private const string BSON_STATE_MEMBER = "@state";
        private const string BSON_VERSIONDATE_MEMBER = "@versionDate";
        private const string BSON_ENTRY_TYPE_MEMBER = "@entryType";
        public const string BSON_COLLECTION_MEMBER = "@collection";
        private const string BSON_BATCHID_MEMBER = "@batchId";

        private const string BSON_ID_MEMBER = "id";
        private const string BSON_RECORDID_MEMBER = "_id";      // SelfLink is re-used as the Mongo key
       // private const string BSON_CONTENT_MEMBER = "content";

        private const string BSON_STATE_CURRENT = "current";
        private const string BSON_STATE_SUPERCEDED = "superceded";

        public const string RESOURCE_COLLECTION = "resources";
        private const string COUNTERS_COLLECTION = "counters";
        private const string SNAPSHOT_COLLECTION = "snapshots";

        public MongoFhirStore(MongoDatabase database)
        {
            this.database = database;
        }
        /// <summary>
        /// Retrieves an Entry by its id. This includes content and deleted entries.
        /// </summary>
        /// <param name="url"></param>
        /// <returns>An entry, including full content, or null if there was no entry with the given id</returns>
        public BundleEntry FindEntryById(Uri url)
        {
            var found = findCurrentDocumentById(url.ToString());

            if (found == null) return null;

            return reconstituteBundleEntry(found, fetchContent: true);
        }

        private IMongoQuery makeFindCurrentByIdQuery(string url)
        {
            return MonQ.Query.And(
                    MonQ.Query.EQ(BSON_ID_MEMBER, url),
                    MonQ.Query.EQ(BSON_STATE_MEMBER, BSON_STATE_CURRENT)
                    );
        }

        private IMongoQuery makeFindCurrentByIdQuery(IEnumerable<Uri> urls)
        {
            return MonQ.Query.And(
                    MonQ.Query.In(BSON_ID_MEMBER, urls.Select(url => BsonValue.Create(url.ToString()))),
                    MonQ.Query.EQ(BSON_STATE_MEMBER, BSON_STATE_CURRENT)
                    );
        }

        private BsonDocument findCurrentDocumentById(string url)
        {
            var coll = getResourceCollection();

            return coll.FindOne(makeFindCurrentByIdQuery(url));
        }

        /// <summary>
        /// Retrieves a specific version of an Entry. Includes content and deleted entries.
        /// </summary>
        /// <param name="url"></param>
        /// <returns>An entry, including full content, or null if there was no entry with the given id</returns>
        public BundleEntry FindVersionByVersionId(Uri url)
        {
            var coll = getResourceCollection();

            var found = coll.FindOne(MonQ.Query.EQ(BSON_RECORDID_MEMBER, url.ToString()));

            if (found == null) return null;

            return reconstituteBundleEntry(found, fetchContent: true);
        }

        public IEnumerable<BundleEntry> FindByVersionIds(IEnumerable<Uri> entryVersionIds)
        {
            var coll = getResourceCollection();

            var keys = entryVersionIds.Select(uri => (BsonValue)uri.ToString());
            var query = MonQ.Query.In(BSON_RECORDID_MEMBER, keys);
            var entryDocs = coll.Find(query);

            // Unfortunately, Query.IN returns the entries out of order with respect to the set of id's
            // passed to it as a parameter...we have to resort.
            var sortedDocs = entryDocs.OrderBy(doc => doc[BSON_RECORDID_MEMBER].ToString(),
                    new SnapshotSorter(entryVersionIds.Select(vi => vi.ToString())));

            return sortedDocs.Select(doc => reconstituteBundleEntry(doc, fetchContent: true));
        }

        private class SnapshotSorter : IComparer<string>
        {
            Dictionary<string, int> keyPositions = new Dictionary<string, int>();

            public SnapshotSorter(IEnumerable<string> keys)
            {
                int index = 0;
                foreach (var key in keys)
                {
                    keyPositions.Add(key, index);
                    index += 1;
                }
            }

            public int Compare(string a, string b)
            {
                return keyPositions[a] - keyPositions[b];
            }
        }

        public IEnumerable<BundleEntry> ListCollection(string collectionName, bool includeDeleted = false,
                                DateTimeOffset? since = null, int limit = 100)
        {
            return findAll(limit, since, onlyCurrent: true, includeDeleted: includeDeleted, collection: collectionName);
        }

        public IEnumerable<BundleEntry> ListVersionsInCollection(string collectionName, DateTimeOffset? since = null,
                            int limit = 100)
        {
            return findAll(limit, since, onlyCurrent: false, includeDeleted: true, collection: collectionName);
        }

        public IEnumerable<BundleEntry> ListVersionsById(Uri url, DateTimeOffset? since = null,
                                    int limit = 100)
        {
            return findAll(limit, since, onlyCurrent: false, includeDeleted: true, id: url);
        }

        public IEnumerable<BundleEntry> ListVersions(DateTimeOffset? since = null, int limit = 100)
        {
            return findAll(limit, since, onlyCurrent: false, includeDeleted: true);
        }

        private IEnumerable<BundleEntry> findAll(int limit, DateTimeOffset? since, bool onlyCurrent, bool includeDeleted,
                                    Uri id = null, string collection = null)
        {
            DateTime? mongoSince = convertDateTimeOffsetToDateTime(since);       // Stored in mongo, corrected for Utc

            var coll = getResourceCollection();

            var queries = new List<IMongoQuery>();

            if (mongoSince != null)
                queries.Add(MonQ.Query.GT(BSON_VERSIONDATE_MEMBER, mongoSince));
            if (onlyCurrent)
                queries.Add(MonQ.Query.EQ(BSON_STATE_MEMBER, BSON_STATE_CURRENT));
            if (!includeDeleted)
                queries.Add(MonQ.Query.NE(BSON_ENTRY_TYPE_MEMBER, typeof(DeletedEntry).Name));
            if (id != null)
                queries.Add(MonQ.Query.EQ(BSON_ID_MEMBER, id.ToString()));
            if (collection != null)
                queries.Add(MonQ.Query.EQ(BSON_COLLECTION_MEMBER, collection));

            MongoCursor<BsonDocument> cursor = null;

            if (queries.Count > 0)
                cursor = coll.Find(MonQ.Query.And(queries));
            else
                cursor = coll.FindAll();

            // Get a subset of the list, and don't include the Content field (where the resource data is)
            var result = cursor
                    .SetFields(MonQ.Fields.Exclude("Content"))
                    .SetSortOrder(MonQ.SortBy.Descending(BSON_VERSIONDATE_MEMBER))
                    .Take(limit);

            // Return the entries without Resource or binary content. These can be fetched later
            // using the other calls.
            return result.Select(doc => reconstituteBundleEntry(doc, fetchContent: false));
        }

        //private BundleEntry clone(BundleEntry entry)
        //{
        //    ErrorList err = new ErrorList();

        //    var xml = FhirSerializer.SerializeBundleEntryToXml(entry);
        //    var result = FhirParser.ParseBundleEntryFromXml(xml, err);

        //    if (err.Count > 0)
        //        throw new InvalidOperationException("Unexpected parse error while cloning: " + err.ToString());

        //    return result;
        //}


        public BundleEntry AddEntry(BundleEntry entry, Guid? batchId = null)
        {
            if (entry == null) throw new ArgumentNullException("entry");

            return AddEntries(new BundleEntry[] { entry }, batchId).FirstOrDefault();

        }

        public IEnumerable<BundleEntry> AddEntries(IEnumerable<BundleEntry> entries, Guid? batchId = null)
        {
            if (entries == null) throw new ArgumentNullException("entries");

            if (entries.Count() == 0) return entries;   // return an empty set

            if (entries.Any(entry => entry.Id == null))
                throw new ArgumentException("All entries must have an entry id");
            
            if (entries.Any(entry => entry.Links.SelfLink == null))
                throw new ArgumentException("All entries must have a selflink");

            foreach (var entry in entries)
                updateTimestamp(entry);

            save(entries, batchId);

            return entries;
        }

        private static void updateTimestamp(BundleEntry entry)
        {
            if (entry is DeletedEntry)
                ((DeletedEntry)entry).When = DateTimeOffset.Now.ToUniversalTime();
            else
                ((ResourceEntry)entry).LastUpdated = DateTimeOffset.Now.ToUniversalTime();
        }


        public void ReplaceEntry(ResourceEntry entry, Guid? batchId = null)
        {
            if (entry == null) throw new ArgumentNullException("entry");
            if (entry.SelfLink == null) throw new ArgumentException("entry.SelfLink");

            var query = MonQ.Query.EQ(BSON_RECORDID_MEMBER, entry.SelfLink.ToString());

            if(batchId == null) batchId = Guid.NewGuid();
            updateTimestamp(entry);

            var doc = entryToBsonDocument(entry);
            doc[BSON_STATE_MEMBER] = BSON_STATE_CURRENT;
            doc[BSON_BATCHID_MEMBER] = batchId.ToString();

            var coll = getResourceCollection();

            coll.Remove(query);
            coll.Save(doc);            
        }
        
        // This used to be an anonymous function. Should it be merged with entryToBsonDocument ? /mh
        private BsonDocument createDocFromEntry(BundleEntry entry, Guid batchId)
        {
            try
            {
                // Externalize binary content to AmazonS3
                byte[] originalContent = null;
                if (entry is ResourceEntry<Binary>)
                {
                    var be = (ResourceEntry<Binary>)entry;
                    originalContent = be.Resource.Content;

                    externalizeBinaryContents(be);
                 
                    be.Resource.Content = null;
                }

                // Note: temporarily, we made the binary body null, so it does not get
                // serialized..unnecessary because we externalized it so Amazon S3
                var doc = entryToBsonDocument(entry);

                if (entry is ResourceEntry<Binary>)
                    ((ResourceEntry<Binary>)entry).Resource.Content = originalContent;

                doc[BSON_STATE_MEMBER] = BSON_STATE_CURRENT;
                doc[BSON_BATCHID_MEMBER] = batchId.ToString();

                return doc;
            }
            catch
            {
                throw; // Temporary. Add breakpoint to catch exceptions in Fhir Serialize
            }
        }

        private bool isQuery(BundleEntry entry)
        {
            if (entry is ResourceEntry)
            {
                return !((entry as ResourceEntry).Resource is Query);
            }
            else return true;
        }

        private void markSuperceded(IEnumerable<Uri> list)
        {
            
            var query = makeFindCurrentByIdQuery(list);
            ResourceCollection.Update(query, MonQ.Update.Set(BSON_STATE_MEMBER, BSON_STATE_SUPERCEDED), UpdateFlags.Multi);
        }

        private void unmarkSuperceded(IEnumerable<Uri> list)
        {
            // todo: BUG - this is not going to work. because "makeFindCurrentByIdQuery" assumes a BSON_STATE_CURRENT!!!
            ResourceCollection.Update(
                    makeFindCurrentByIdQuery(list),
                    MonQ.Update.Set(BSON_STATE_MEMBER, BSON_STATE_CURRENT), UpdateFlags.Multi);
        }

        private void storeBinaryContents(IEnumerable<BundleEntry> entries)
        {
            foreach (BundleEntry entry in entries)
            {
                if (entry is ResourceEntry<Binary>)
                {
                    var be = (ResourceEntry<Binary>)entry;
                    externalizeBinaryContents(be);
                }
            }
        }

        private void clearBinaryContents(IEnumerable<BundleEntry> entries)
        {
            foreach (BundleEntry entry in entries)
            {
                if (entry is ResourceEntry<Binary>)
                {
                    var be = (ResourceEntry<Binary>)entry;
                    be.Resource.Content = null;
                }
            }
        }

        /// <summary>
        /// Saves a set of entries to the store, marking existing entries with the
        /// same entry id as superceded.
        /// </summary>
        private void save(IEnumerable<BundleEntry> entries, Guid? batchId = null)
        {
            Guid _batchId = batchId ?? Guid.NewGuid();

            List<BundleEntry> _entries = entries.Where(e => isQuery(e)).ToList();
            storeBinaryContents(_entries);
            clearBinaryContents(_entries);

            IEnumerable<BsonDocument> docs = _entries.Select(e => createDocFromEntry(e, _batchId));
            IEnumerable<Uri> idlist = _entries.Select(e => e.Id);

            markSuperceded(idlist);

            try
            {
                ResourceCollection.InsertBatch(docs);
            }
            catch
            {
                // ROLLBACK
                // Oops. Save failed. Since we don't have transactions, try
                // to revert the state of the existing entries back to current.
                unmarkSuperceded(idlist);

                //TODO: Also remove from amazon

                throw;
            }
        }

        private IBlobStorage getBlobStorage()
        {
            return DependencyCoupler.Inject<IBlobStorage>();
        }
        private void externalizeBinaryContents(ResourceEntry<Binary> be)
        {
            if (be.Resource.ContentType == null)
                throw new ArgumentException("Entry is a binary entry with content, but the entry does not supply a mediatype");

            if (be.Resource.Content != null)
            {
                using (var blobStore = getBlobStorage())
                {
                    var blobId = calculateBlobName(be.Links.SelfLink);

                    blobStore.Open();
                    blobStore.Store(blobId, new MemoryStream(be.Resource.Content));
                }
            }
        }

        public void PurgeBatch(Guid batchId)
        {
            var coll = getResourceCollection();
            var batchQry = MonQ.Query.EQ(BSON_BATCHID_MEMBER, batchId.ToString());

            var batchMembers = coll.Find(batchQry)
                    .SetFields(MonQ.Fields.Include(BSON_RECORDID_MEMBER))
                    .Select(doc => calculateBlobName(new Uri(doc[BSON_RECORDID_MEMBER].ToString())));

            using (var blobStore = getBlobStorage())
            {
                blobStore.Open();
                blobStore.Delete(batchMembers);
            }

            coll.Remove(MonQ.Query.EQ(BSON_BATCHID_MEMBER, batchId.ToString()));
        }

        private DateTime? convertDateTimeOffsetToDateTime(DateTimeOffset? date)
        {
            return date != null ? date.Value.UtcDateTime : (DateTime?)null;
        }

        private BsonDocument entryToBsonDocument(BundleEntry entry)
        {
            try
            { 
                var docJson = FhirSerializer.SerializeBundleEntryToJson(entry);
                var doc = BsonDocument.Parse(docJson);

                doc[BSON_RECORDID_MEMBER] = entry.Links.SelfLink.ToString();
                doc[BSON_ENTRY_TYPE_MEMBER] = getEntryTypeFromInstance(entry);
                doc[BSON_COLLECTION_MEMBER] = new ResourceIdentity(entry.Id).Collection;

                if (entry is ResourceEntry)
                {
                    doc[BSON_VERSIONDATE_MEMBER] = convertDateTimeOffsetToDateTime(((ResourceEntry)entry).LastUpdated);
                    //                doc[BSON_VERSIONDATE_MEMBER_ISO] = Util.FormatIsoDateTime(((ContentEntry)entry).LastUpdated.Value.ToUniversalTime());
                }
                if (entry is DeletedEntry)
                {
                    doc[BSON_VERSIONDATE_MEMBER] = convertDateTimeOffsetToDateTime(((DeletedEntry)entry).When);
                    //                doc[BSON_VERSIONDATE_MEMBER_ISO] = Util.FormatIsoDateTime(((DeletedEntry)entry).When.Value.ToUniversalTime());
                }

                return doc;
            }
                catch
            {
                throw; // sorry. dit gedaan, omdat we de fout anders niet vinden in de serializer.
            }
        }

        private string getEntryTypeFromInstance(BundleEntry entry)
        {
            if (entry is DeletedEntry)
                return "DeletedEntry";
            else if (entry is ResourceEntry)
                return "ResourceEntry";
            else
                throw new ArgumentException("Unsupported BundleEntry type: " + entry.GetType().Name);
        }

        private string calculateBlobName(Uri selflink)
        {
            var rl = new ResourceIdentity(selflink);

            return rl.Collection + "/" + rl.Id + "/" + rl.VersionId;
        }

        public void StoreSnapshot(Snapshot snap)
        {
            var coll = database.GetCollection(SNAPSHOT_COLLECTION);

            coll.Save<Snapshot>(snap);
        }

        public Snapshot GetSnapshot(string snapshotId)
        {
            var coll = database.GetCollection(SNAPSHOT_COLLECTION);

            return coll.FindOneByIdAs<Snapshot>(snapshotId);
        }

        public IEnumerable<Tag> ParseToTags(IEnumerable<BsonValue> items)
        {
            List<Tag> tags = new List<Tag>();
            foreach (BsonDocument item in items)
            {
                Tag tag = new Tag(
                    item["term"].AsString,
                    new Uri(item["scheme"].AsString),
                    item["label"].AsString);

                tags.Add(tag);
            }
            return tags;
        }

        public IEnumerable<Tag> ListTagsInServer()
        {
            IEnumerable<BsonValue> items = ResourceCollection.Distinct("category");
            return ParseToTags(items);
        }
        
        public IEnumerable<Tag> ListTagsInCollection(string collection)
        {
            IMongoQuery query = MonQ.Query.EQ(BSON_COLLECTION_MEMBER, collection);
            IEnumerable<BsonValue> items = ResourceCollection.Distinct("category", query);
            return ParseToTags(items);
        }

        public int GenerateNewIdSequenceNumber()
        {

            var coll = database.GetCollection(COUNTERS_COLLECTION);

            var resourceIdQuery = MonQ.Query.EQ("_id", "resourceId");
            var newId = coll.FindAndModify(resourceIdQuery, null, MonQ.Update.Inc("last", 1), true, true);

            return newId.ModifiedDocument["last"].AsInt32;
        }

        public void EnsureNextSequenceNumberHigherThan(int seq)
        {
            var coll = database.GetCollection(COUNTERS_COLLECTION);

            if (coll.FindOne(MonQ.Query.EQ("_id", "resourceId")) == null)
            {
                coll.Insert(
                    new BsonDocument(new List<BsonElement>()
                    {
                        new BsonElement("_id", "resourceId"),
                        new BsonElement("last", seq)
                    }
                ));
            }
            else
            {
                var resourceIdQuery =
                    MonQ.Query.And(MonQ.Query.EQ("_id", "resourceId"), MonQ.Query.LTE("last", seq));

                coll.FindAndModify(resourceIdQuery, null, MonQ.Update.Set("last",seq));
            }
        }

        public int GenerateNewVersionSequenceNumber()
        {
            var coll = database.GetCollection(COUNTERS_COLLECTION);

            var resourceIdQuery = MonQ.Query.EQ("_id", "versionId");
            var newId = coll.FindAndModify(resourceIdQuery, null, MonQ.Update.Inc("last", 1), true, true);

            return newId.ModifiedDocument["last"].AsInt32;
        }

        // Drops all collections, including the special 'counters' collection for generating ids,
        // AND the binaries stored at Amazon S3
        private void EraseData()
        {
            // Don't try this at home
            var collectionsToDrop = new string[] { RESOURCE_COLLECTION, COUNTERS_COLLECTION, SNAPSHOT_COLLECTION };

            foreach (var collName in collectionsToDrop)
            {
                database.DropCollection(collName);
            }

            using (var blobStorage = getBlobStorage())
            {
                blobStorage.Open();
                blobStorage.DeleteAll();
                blobStorage.Close();
            }
        }

        private void EnsureIndices()
        {
            var coll = getResourceCollection();

            coll.EnsureIndex(BSON_STATE_MEMBER, BSON_ENTRY_TYPE_MEMBER, BSON_COLLECTION_MEMBER);
            coll.EnsureIndex(BSON_ID_MEMBER, BSON_STATE_MEMBER);

            // Should support ListVersions() and ListVersionsInCollection()
            var versionKeys = MonQ.IndexKeys.Descending(BSON_VERSIONDATE_MEMBER).Ascending(BSON_COLLECTION_MEMBER);
            coll.EnsureIndex(versionKeys);

            //            versionKeys = IndexKeys.Descending(BSON_VERSIONDATE_MEMBER_ISO).Ascending(BSON_COLLECTION_MEMBER);
            //            coll.EnsureIndex(versionKeys);

        }

        /// <summary>
        /// Does a complete wipe and reset of the database. USE WITH CAUTION!
        /// </summary>
        public void Clean()
        {
            EraseData();
            EnsureIndices();
        }

        private BundleEntry reconstituteBundleEntry(BsonDocument doc, bool fetchContent)
        {
            if (doc == null) return null;

            // Remove our storage metadata before deserializing
            doc.Remove(BSON_VERSIONDATE_MEMBER);
            //            doc.Remove(BSON_VERSIONDATE_MEMBER_ISO);
            doc.Remove(BSON_STATE_MEMBER);
            doc.Remove(BSON_RECORDID_MEMBER);
            doc.Remove(BSON_ENTRY_TYPE_MEMBER);
            doc.Remove(BSON_COLLECTION_MEMBER);
            doc.Remove(BSON_BATCHID_MEMBER);

            var json = doc.ToJson();
            
            BundleEntry e;
            try
            {
                e = FhirParser.ParseBundleEntryFromJson(json);
            }
            catch (Exception inner)
            {
                throw new InvalidOperationException("Cannot parse MongoDb's json into a feed entry: ", inner);
            }

            if (fetchContent == true)
            {
                if (e is ResourceEntry<Binary>)
                {
                    var be = (ResourceEntry<Binary>)e;

                    var blobId = calculateBlobName(be.Links.SelfLink);

                    using(var blobStorage = getBlobStorage())
                    {
                        blobStorage.Open();
                        be.Resource.Content = blobStorage.Fetch(blobId);
                    }
                }
            }

            return e;
        }

        private MongoCollection<BsonDocument> getResourceCollection()
        {
            return database.GetCollection(RESOURCE_COLLECTION);
        }

        private MongoCollection<BsonDocument> ResourceCollection
        {
            get
            {
                return database.GetCollection(RESOURCE_COLLECTION);
            }
        }
    }
}
