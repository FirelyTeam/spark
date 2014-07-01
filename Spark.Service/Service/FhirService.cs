/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Config;
using Spark.Controllers;
using Spark.Data;
using Spark.Search;
using Spark.Support;
using Spark.Core;
using Hl7.Fhir.Validation;
using Hl7.Fhir.Search;
using Hl7.Fhir.Serialization;

namespace Spark.Service
{
    // todo: ResourceImporter and resourceExporter are provisionally.

    public class FhirService : IFhirService
    {
        private IFhirStore store;
        private IFhirIndex _index;
        private ResourceImporter _importer = null;
        private ResourceExporter exporter = null;
        private Pager pager;
        public Uri Endpoint { get; private set; }


        public FhirService(Uri serviceBase)
        {
            store = DependencyCoupler.Inject<IFhirStore>(); // new MongoFhirStore();
            _index = DependencyCoupler.Inject<IFhirIndex>(); // Factory.Index;
            _importer = DependencyCoupler.Inject<ResourceImporter>();
            exporter = DependencyCoupler.Inject<ResourceExporter>();
            pager = new Pager(store);
            Endpoint = serviceBase;
        }

        private string getNewId()
        {
            return store.GenerateNewIdSequenceNumber().ToString();
        }
        private bool entryExists(string collection, string id)
        {
            // bool should be: status: exists, nonexistent, deleted?

            Uri location = ResourceIdentity.Build(collection, id);
            BundleEntry existing = store.FindEntryById(location);
            return (existing != null);
        }
        
        private BundleEntry findEntry(string collection, string id)
        {
            Uri location = ResourceIdentity.Build(collection, id);
            return store.FindEntryById(location);
        }
        
        private Bundle exportPagedBundle(Bundle bundle, int pagesize = Const.DEFAULT_PAGE_SIZE)
        {
            Bundle result = pager.FirstPage(bundle, pagesize);
            exporter.EnsureAbsoluteUris(result);
            return result;
        }

        private Bundle exportPagedSnapshot(Snapshot snapshot, int pagesize = Const.DEFAULT_PAGE_SIZE)
        {
            Bundle result = pager.FirstPage(snapshot, pagesize);
            exporter.EnsureAbsoluteUris(result);
            return result;
        }

        private ResourceEntry internalCreate(ResourceEntry internalEntry)
        {
            ResourceEntry entry = (ResourceEntry)store.AddEntry(internalEntry);
            _index.Process(internalEntry);

            return entry;
        }

        private ResourceEntry internalRead(string collection, string id)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);

            Uri uri = ResourceIdentity.Build(collection, id);
            BundleEntry entry = store.FindEntryById(uri);

            if (entry == null) 
                throwNotFound("Cannot read resource", collection, id);
            else if (entry is DeletedEntry)
            {
                var deletedentry = (entry as DeletedEntry);
                var message = String.Format("A {0} resource with id {1} existed, but was deleted on {2} (version {3}).",
                  collection, id, deletedentry.When, new ResourceIdentity(deletedentry.Links.SelfLink).VersionId);

                throw new SparkException(HttpStatusCode.Gone, message);
            }
            return (ResourceEntry)entry;
        }

        private void throwNotFound(string intro, string collection, string id, string vid=null)
        {
            if(vid == null)
                throw new SparkException(HttpStatusCode.NotFound, "{0}: No {1} resource with id {2} was found.", intro, collection, id);
            else
                throw new SparkException(HttpStatusCode.NotFound, "{0}: There is no {1} resource with id {2}, or there is no version {3}", intro, collection, id, vid);
        }


        private ResourceEntry internalVRead(string collection, string id, string vid)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);
            RequestValidator.ValidateVersionId(vid);

            var versionUri = ResourceIdentity.Build(collection, id, vid);

            BundleEntry entry = store.FindVersionByVersionId(versionUri);

            if (entry == null)
                throwNotFound("Cannot read version of resource", collection, id, vid);
            else if (entry is DeletedEntry)
                throw new SparkException(HttpStatusCode.Gone,
                    "A {0} resource with version {1} and id {2} exists, but is a deletion (deleted on {3}).",
                    collection, vid, id, (entry as DeletedEntry).When);

            return (ResourceEntry)entry;
        }

        /// <summary>
        /// Retrieves the current contents of a resource.
        /// </summary>
        /// <param name="collection">The resource type, in lowercase</param>
        /// <param name="id">The id part of a Resource id</param>
        /// <returns>A Result containing the resource, or an Issue</returns>
        /// <remarks>
        /// Note: Unknown resources and deleted resources are treated differently on a read: 
        ///   * A deleted resource returns a 410 status code
        ///   * an unknown resource returns 404. 
        /// </remarks>
        public ResourceEntry Read(string collection, string id)
        {
            ResourceEntry entry = internalRead(collection, id);
            exporter.EnsureAbsoluteUris(entry);
            return entry;
        }

        /// <summary>
        /// Read the state of a specific version of the resource.
        /// </summary>
        /// <param name="collectionName">The resource type, in lowercase</param>
        /// <param name="id">The id part of a version-specific reference</param>
        /// <param name="version">The version part of a version-specific reference</param>
        /// <returns>A Result containing the resource, or an Issue</returns>
        /// <remarks>
        /// If the version referred to is actually one where the resource was deleted, the server should return a 
        /// 410 status code. 
        /// </remarks>
        public ResourceEntry VRead(string collection, string id, string version)
        {
            ResourceEntry entry = internalVRead(collection, id, version);
            exporter.EnsureAbsoluteUris(entry);
            return entry;
        }

        /// <summary>
        /// Create a new resource with a server assigned id.
        /// </summary>
        /// <param name="collection">The resource type, in lowercase</param>
        /// <param name="resource">The data for the Resource to be created</param>
        /// <remarks>
        /// May return:
        ///     201 Created - on successful creation
        /// </remarks>
        public ResourceEntry Create(string collection, ResourceEntry entry, string newId = null)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateIdPattern(newId);
            RequestValidator.ValidateResourceBody(entry, collection);

            if (newId == null) newId = getNewId();
            
            ResourceIdentity identity = ResourceIdentity.Build(Endpoint, collection, newId);
            var newEntry = _importer.Import(identity, entry);
                 
            ResourceEntry result = internalCreate(newEntry);
            exporter.EnsureAbsoluteUris(result);

            return result;
        }

        public Bundle Search(string collection, IEnumerable<Tuple<string, string>> parameters, int pageSize)
        {
            RequestValidator.ValidateCollectionName(collection);

            string title = String.Format("Search on resources in collection '{0}'", collection);

            Query query = FhirParser.ParseQueryFromUriParameters(collection, parameters);
            
            ICollection<string> includes = query.Includes;
            
            SearchResults results = _index.Search(query);

            if (results.HasErrors)
            {
                throw new SparkException(HttpStatusCode.BadRequest, results.Outcome);
            }
            RestUrl selfLink = new RestUrl(Endpoint).AddPath(collection).AddPath(results.UsedParameters);
            Snapshot snapshot = Snapshot.Create(title, selfLink.Uri, includes, results, results.MatchCount);

            Bundle bundle = pager.FirstPage(snapshot, pageSize); //TODO: This replaces the selflink with a link to the snapshot...
            store.Include(bundle, includes);

            if (results.HasIssues)
            {
                var outcomeEntry = BundleEntryFactory.CreateFromResource(results.Outcome, new Uri("outcome/1", UriKind.Relative), DateTimeOffset.Now);
                outcomeEntry.SelfLink = outcomeEntry.Id;
                bundle.Entries.Add(outcomeEntry);
            }

            exporter.EnsureAbsoluteUris(bundle);
            return bundle;
        }
         
        public ResourceEntry Update(string collection, string id, ResourceEntry entry, Uri updatedVersionUri = null)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);
            RequestValidator.ValidateResourceBody(entry, collection);

            BundleEntry current = findEntry(collection, id);
            if (current == null) return null;

            // todo: this fails. Both selflink and updatedVersionUri can be empty, but this function requires both.
                // Check if update done against correct version, if applicable
                // RequestValidator.ValidateCorrectUpdate(entry.Links.SelfLink, updatedVersionUri); // can throw exception
            
            // Entry already exists, add a new ResourceEntry with the same id
            var identity = ResourceIdentity.Build(Endpoint, collection, id);

            ResourceEntry newEntry = _importer.Import(identity, entry);
                    //_importer.QueueNewResourceEntry(identity, entry.Resource);
                    //var newEntry = (ResourceEntry)_importer.ImportQueued().First();

            // Merge tags passed to the update with already existing tags.
            newEntry.Tags = _importer.AffixTags(current, newEntry);

            var newVersion = store.AddEntry(newEntry);
            _index.Process(newVersion);

            exporter.EnsureAbsoluteUris(newVersion);
            return (ResourceEntry)newVersion;
        }

        /// <summary>
        /// Delete a resource.
        /// </summary>
        /// <param name="collection">The resource type, in lowercase</param>
        /// <param name="id">The id part of a Resource id</param>
        /// <remarks>
        /// Upon successful deletion the server should return 
        ///   * 204 (No Content). 
        ///   * If the resource does not exist on the server, the server must return 404 (Not found).
        ///   * Performing this operation on a resource that is already deleted has no effect, and should return 204 (No Content).
        /// </remarks>
        public void Delete(string collection, string id)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);

            // See if we already have a version on store

            BundleEntry current = findEntry(collection, id);
                //var identity = resourceidentity.build(collection, id);
                //bundleentry current = _store.findentrybyid(identity);

            if (current == null)
            {
                throw new SparkException(HttpStatusCode.NotFound,
                    "No {0} resource with id {1} was found, so it cannot be deleted.", collection, id);
            }
            else if (!(current is DeletedEntry))
            {
                // Add a new deleted-entry to mark this entry as deleted
                _importer.QueueNewDeletedEntry(collection, id);
                BundleEntry deletedEntry = _importer.ImportQueued().First();

                store.AddEntry(deletedEntry);
                _index.Process(deletedEntry);
            }

        }

        public Bundle Transaction(Bundle bundle)
        {
            IEnumerable<BundleEntry> entries = _importer.Import(bundle.Entries);
            
            Guid transaction = Guid.NewGuid();
            try
            {
                entries = store.AddEntries(entries, transaction);
                _index.Process(entries);

                exporter.RemoveBodyFromEntries(entries);
                bundle.Entries = entries.ToList();
                
                exporter.EnsureAbsoluteUris(bundle);
                return bundle;
            }
            catch
            {
                // todo: Purge batch from index 
                store.PurgeBatch(transaction);
                throw;
            }
        }
        
        public Bundle History(DateTimeOffset? since)
        {
            if (since == null) since = DateTimeOffset.MinValue;
            string title = String.Format("Full server-wide history for updates since {0}", since);
            RestUrl self = new RestUrl(this.Endpoint).AddPath(RestOperation.HISTORY);

            /*IEnumerable<BundleEntry> entries = store.ListVersions(since, Const.MAX_HISTORY_RESULT_SIZE);
            Snapshot snapshot = Snapshot.Create(title, self.Uri, null, entries, entries.Count());
            */
            
            ICollection<Uri> keys = store.HistoryKeys(since);
            Snapshot snapshot = Snapshot.Create(title, self.Uri, null, keys, keys.Count());
            

            Bundle bundle = pager.FirstPage(snapshot, Const.DEFAULT_PAGE_SIZE);
            exporter.EnsureAbsoluteUris(bundle);
            return bundle;
        }




        public Bundle History(string collection, DateTimeOffset? since)
        {
            RequestValidator.ValidateCollectionName(collection);
            if (since == null) since = DateTimeOffset.MinValue;
            string title = String.Format("Full server-wide history for updates since {0}", since);
            RestUrl self = new RestUrl(this.Endpoint).AddPath(collection, RestOperation.HISTORY);

            IEnumerable<BundleEntry> entries = store.ListVersionsInCollection(collection, since, Const.MAX_HISTORY_RESULT_SIZE);
           // Bundle bundle = BundleEntryFactory.CreateBundleWithEntries(title, self.Uri, Const.AUTHOR, Settings.AuthorUri, entries);
            var snapshot = Snapshot.Create(title, self.Uri, null, entries, Snapshot.NOCOUNT);
            return exportPagedSnapshot(snapshot);
        }

        public Bundle History(string collection, string id, DateTimeOffset? since)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);

            if (since == null) since = DateTimeOffset.MinValue;
            string title = String.Format("History for updates on '{0}' resource '{1}' since {2}", collection, id, since);
            RestUrl self = new RestUrl(this.Endpoint).AddPath(collection, id, RestOperation.HISTORY);

            if (!entryExists(collection, id))
                throw new SparkException(HttpStatusCode.NotFound, "There is no history because there is no {0} resource with id {1}.", collection, id);

            var identity = ResourceIdentity.Build(collection, id).OperationPath;
            IEnumerable<BundleEntry> entries = store.ListVersionsById(identity, since, Const.MAX_HISTORY_RESULT_SIZE);
            //Bundle bundle = BundleEntryFactory.CreateBundleWithEntries(title, self.Uri, Const.AUTHOR, Settings.AuthorUri, entries);

            var snapshot = Snapshot.Create(title, self.Uri, null, entries, Snapshot.NOCOUNT);
            return exportPagedSnapshot(snapshot);
        }

        
        public Bundle Mailbox(Bundle bundle, Binary body)
        {
            // todo: this is not DSTU-1 conformant. 
            if(bundle == null || body == null) throw new SparkException("Mailbox requires a Bundle body payload"); 
            // For the connectathon, this *must* be a document bundle
            if (bundle.GetBundleType() != BundleType.Document)
                throw new SparkException("Mailbox endpoint currently only accepts Document feeds");

            Bundle result = new Bundle("Transaction result from posting of Document " + bundle.Id, DateTimeOffset.Now);

            // Build a binary with the original body content (=the unparsed Document)
            var binaryEntry = new ResourceEntry<Binary>(new Uri("cid:" + Guid.NewGuid()), DateTimeOffset.Now, body);
            binaryEntry.SelfLink = new Uri("cid:" + Guid.NewGuid());

            // Build a new DocumentReference based on the 1 composition in the bundle, referring to the binary
            var compositions = bundle.Entries.OfType<ResourceEntry<Composition>>();
            if (compositions.Count() != 1) throw new SparkException("Document feed should contain exactly 1 Composition resource");
            var composition = compositions.First().Resource;
            var reference = ConnectathonDocumentScenario.DocumentToDocumentReference(composition, bundle, body, binaryEntry.SelfLink);

            // Start by copying the original entries to the transaction, minus the Composition
            List<BundleEntry> entriesToInclude = new List<BundleEntry>();

            //TODO: Only include stuff referenced by DocumentReference
            //if(reference.Subject != null) entriesToInclude.AddRange(bundle.Entries.ById(new Uri(reference.Subject.Reference)));
            //if (reference.Author != null) entriesToInclude.AddRange(
            //         reference.Author.Select(auth => bundle.Entries.ById(auth.Id)).Where(be => be != null));
            //reference.Subject = composition.Subject;
            //reference.Author = new List<ResourceReference>(composition.Author);
            //reference.Custodian = composition.Custodian;

            foreach (var entry in bundle.Entries.Where(be => !(be is ResourceEntry<Composition>)))
                result.Entries.Add(entry);

            // Now add the newly constructed DocumentReference and the Binary
            result.Entries.Add(new ResourceEntry<DocumentReference>(new Uri("cid:" + Guid.NewGuid()), DateTimeOffset.Now, reference));
            result.Entries.Add(binaryEntry);

            // Process the constructed bundle as a Transaction and return the result
            return Transaction(result);
        }

        public TagList TagsFromServer()
        {
            IEnumerable<Tag> tags = store.ListTagsInServer();
            return new TagList(tags);
        }
        
        public TagList TagsFromResource(string collection)
        {
            RequestValidator.ValidateCollectionName(collection);
            IEnumerable<Tag> tags = store.ListTagsInCollection(collection);
            return new TagList(tags);
        }

        public TagList TagsFromInstance(string collection, string id)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);

            Uri uri = ResourceIdentity.Build(collection, id);
            BundleEntry entry = store.FindEntryById(uri);

            if (entry == null) throwNotFound("Cannot retrieve tags", collection, id);

             return new TagList(entry.Tags);
         }

        public TagList TagsFromHistory(string collection, string id, string vid)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);
            RequestValidator.ValidateVersionId(vid);

            var uri = ResourceIdentity.Build(collection, id, vid);
            BundleEntry entry = store.FindVersionByVersionId(uri);

            if (entry == null)
                throwNotFound("Cannot retrieve tags", collection, id, vid);            
            else if (entry is DeletedEntry)
            {
                throw new SparkException(HttpStatusCode.Gone,
                    "A {0} resource with version {1} and id {2} exists, but is a deletion (deleted on {3}).",
                    collection, vid, id, (entry as DeletedEntry).When);
            }

            return new TagList(entry.Tags);
        }

        public void AffixTags(string collection, string id, IEnumerable<Tag> tags)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);
            if (tags == null) throw new SparkException("No tags specified on the request");

            ResourceEntry existing = this.internalRead(collection, id);
            existing.Tags = _importer.AffixTags(existing, tags);
            store.ReplaceEntry(existing);
        }

        public void AffixTags(string collection, string id, string vid, IEnumerable<Tag> tags)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);
            RequestValidator.ValidateVersionId(vid);
            if (tags == null) throw new SparkException("No tags specified on the request");

            ResourceEntry existing = this.internalVRead(collection, id, vid);
            existing.Tags = _importer.AffixTags(existing, tags);
            store.ReplaceEntry(existing);   
        }

        public void RemoveTags(string collection, string id, IEnumerable<Tag> tags)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);
            if (tags == null) throw new SparkException("No tags specified on the request");

            ResourceEntry existing = this.internalRead(collection, id);

            if (existing.Tags != null)
                existing.Tags = existing.Tags.Exclude(tags).ToList();

            store.ReplaceEntry(existing);
        }
        public void RemoveTags(string collection, string id, string vid, IEnumerable<Tag> tags)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);
            RequestValidator.ValidateVersionId(vid);
            if (tags == null) throw new SparkException("No tags specified on the request");

            ResourceEntry existing = this.internalVRead(collection, id, vid);

            if (existing.Tags != null)
                existing.Tags = existing.Tags.Exclude(tags).ToList();

            store.ReplaceEntry(existing);
        }

        public void Validate(string collection, ResourceEntry entry)
        {
            RequestValidator.ValidateCollectionName(collection);
            if(entry == null) throw new SparkException("Validate needs a Resource in the body payload");

            entry.Title = "Validation test entity";
            entry.LastUpdated = DateTime.Now;
            entry.Id = ResourceIdentity.Build(Endpoint, collection, getNewId());
            RequestValidator.ValidateEntry(entry);
        }

        public void Validate(string collection, string id, ResourceEntry entry)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);

            if (entry == null) throw new SparkException("Validate needs a Resource in the body payload");

            entry.Title = "Validation test entity";
            entry.LastUpdated = DateTime.Now;
            entry.Id = ResourceIdentity.Build(Endpoint, collection, id);
            RequestValidator.ValidateEntry(entry);
        }
       
        public ResourceEntry Conformance()
        {
            var conformance = ConformanceBuilder.Build();
            var entry = new ResourceEntry<Conformance>(new Uri("urn:guid:" + Guid.NewGuid()), DateTimeOffset.Now, conformance);
            return entry;


            //Uri location =
            //     ResourceIdentity.Build(
            //        ConformanceBuilder.CONFORMANCE_COLLECTION_NAME,
            //        ConformanceBuilder.CONFORMANCE_ID
            //    ).OperationPath;

            //BundleEntry conformance = _store.FindEntryById(location);

            //if (conformance == null || !(conformance is ResourceEntry))
            //{
            //    throw new SparkException(
            //        HttpStatusCode.InternalServerError,
            //        "Cannot find an installed conformance statement for this server. Has it been initialized?");
            //}
            //else
            //    return (ResourceEntry)conformance;
        }

        public Bundle GetSnapshot(string snapshotid, int index, int count)
        {
            Snapshot snapshot = store.GetSnapshot(snapshotid);
            Bundle bundle = pager.GetPage(snapshot, index, count);
            store.Include(bundle, snapshot.Includes);
            exporter.EnsureAbsoluteUris(bundle);
            return bundle;
        }
    }
}