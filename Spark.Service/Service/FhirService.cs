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

    public class FhirService 
    {
        //refac: private IFhirStore store;
        private IFhirStorage store;
        private IFhirIndex index;
        private IGenerator generator;
        private ITagStore tagstore;
        private ResourceImporter importer = null;
        private ResourceExporter exporter = null;
        private Pager pager;
        public Uri Endpoint { get; private set; }

        public FhirService(Uri endpoint)
        {
            //refac: store = DependencyCoupler.Inject<IFhirStore>(); // new MongoFhirStore();

            store = DependencyCoupler.Inject<IFhirStorage>();
            tagstore = DependencyCoupler.Inject<ITagStore>();
            generator = DependencyCoupler.Inject<IGenerator>();
            index = DependencyCoupler.Inject<IFhirIndex>(); // Factory.Index;
            importer = DependencyCoupler.Inject<ResourceImporter>();
            exporter = DependencyCoupler.Inject<ResourceExporter>();
            pager = new Pager(store, exporter);
            Endpoint = endpoint;
        }

        public Uri BuildKey(string collection, string id, string vid = null)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);
            if (vid != null) RequestValidator.ValidateVersionId(vid);
            Uri uri = ResourceIdentity.Build(collection, id, vid).OperationPath;
            return uri;
        }
        
        public bool Exists(string collection, string id)
        {
            Uri key = BuildKey(collection, id);
            return store.Exists(key);
        }
        
        private void throwNotFound(string intro, string collection, string id, string vid=null)
        {
            if(vid == null)
                throw new SparkException(HttpStatusCode.NotFound, "{0}: No {1} resource with id {2} was found.", intro, collection, id);
            else
                throw new SparkException(HttpStatusCode.NotFound, "{0}: There is no {1} resource with id {2}, or there is no version {3}", intro, collection, id, vid);
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
            Uri key = BuildKey(collection, id);

            BundleEntry entry = store.Get(key);

            if (entry == null)
                throwNotFound("Cannot read resource", collection, id);

            else if (entry is DeletedEntry)
            {
                var deletedentry = (entry as DeletedEntry);
                var message = String.Format("A {0} resource with id {1} existed, but was deleted on {2} (version {3}).",
                  collection, id, deletedentry.When, new ResourceIdentity(deletedentry.Links.SelfLink).VersionId);

                throw new SparkException(HttpStatusCode.Gone, message);
            }

            ResourceEntry result = (ResourceEntry)entry;
            exporter.Externalize(result);

            return result;
        }

        /// <summary>
        /// Read the state of a specific version of the resource.
        /// </summary>
        /// <param name="collectionName">The resource type, in lowercase</param>
        /// <param name="id">The id part of a version-specific reference</param>
        /// <param name="vid">The version part of a version-specific reference</param>
        /// <returns>A Result containing the resource, or an Issue</returns>
        /// <remarks>
        /// If the version referred to is actually one where the resource was deleted, the server should return a 
        /// 410 status code. 
        /// </remarks>
        public ResourceEntry VRead(string collection, string id, string vid)
        {
            Uri key = BuildKey(collection, id, vid);

            BundleEntry entry = store.Get(key);

            if (entry == null)
                throwNotFound("Cannot read version of resource", collection, id, vid);

            else if (entry is DeletedEntry)
                throw new SparkException(HttpStatusCode.Gone,
                    "A {0} resource with version {2} and id {1} exists, but is a deletion (deleted on {3}).",
                    collection, id, vid, (entry as DeletedEntry).When);

            ResourceEntry result = (ResourceEntry)entry;
            exporter.Externalize(result);
            return result;
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
        public ResourceEntry Create(ResourceEntry entry, string collection, string id = null)
        {
            RequestValidator.ValidateResourceBody(entry, collection);
            Uri key = BuildKey(collection, id ?? generator.NextKey(entry.Resource));
            entry.Id = key;

            importer.Import(entry);
            store.Add(entry);
            index.Process(entry);

            ResourceEntry result = (ResourceEntry)store.Get(key);
            exporter.Externalize(result);

            return result;
        }

        public Bundle Search(string collection, IEnumerable<Tuple<string, string>> parameters, int pageSize, string sortby)
        {
            RequestValidator.ValidateCollectionName(collection);
            Query query = FhirParser.ParseQueryFromUriParameters(collection, parameters);
            ICollection<string> includes = query.Includes;
            
            SearchResults results = index.Search(query);

            if (results.HasErrors)
            {
                throw new SparkException(HttpStatusCode.BadRequest, results.Outcome);
            }
            
            RestUrl selfLink = new RestUrl(Endpoint).AddPath(collection).AddPath(results.UsedParameters);
            string title = String.Format("Search on resources in collection '{0}'", collection);
            Snapshot snapshot = Snapshot.Create(title, selfLink.Uri, results, sortby, includes);
            store.AddSnapshot(snapshot);

            Bundle bundle = pager.GetPage(snapshot, 0, pageSize);

            /*
            if (results.HasIssues)
            {
                var outcomeEntry = BundleEntryFactory.CreateFromResource(results.Outcome, new Uri("outcome/1", UriKind.Relative), DateTimeOffset.Now);
                outcomeEntry.SelfLink = outcomeEntry.Id;
                bundle.Entries.Add(outcomeEntry);
            }
            */

            exporter.Externalize(bundle);
            return bundle;
        }

        
        public ResourceEntry Update(ResourceEntry entry, string collection, string id, Uri updatedVersionUri = null)
        {
            RequestValidator.ValidateResourceBody(entry, collection);
            Uri key = BuildKey(collection, id);

            // Does the entry exist?
            BundleEntry current = store.Get(key);
            
            if (current == null) 
                throw new SparkException(HttpStatusCode.BadRequest , "Cannot update a resource {0} with id {1}, because it doesn't exist on this server", collection, id);

            RequestValidator.ValidateVersion(entry, current);

            // Prepare the entry for storage
            entry.Id = key; //entry.OverloadKey(key);
            BundleEntry newentry = importer.Import(entry);
            newentry.Tags = current.Tags.Affix(newentry.Tags).ToList();

            store.Add(newentry);
            index.Process(newentry);

            exporter.Externalize(newentry);
            return (ResourceEntry)newentry;
        }
        
        public ResourceEntry Upsert(ResourceEntry entry, string collection, string id)
        {
            Uri key = BuildKey(collection, id);
            if (store.Exists(key))
            {
                return Update(entry, collection, id);
            }
            else
            {
                return Create(entry, collection, id);
            }
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
            Uri key = BuildKey(collection, id);

            BundleEntry current = store.Get(key);

            if (current == null)
            {
                throw new SparkException(HttpStatusCode.NotFound,
                    "No {0} resource with id {1} was found, so it cannot be deleted.", collection, id);
            }
            else if (!(current is DeletedEntry))
            {
                // Add a new deleted-entry to mark this entry as deleted
                importer.EnqueueDelete(key);
                IEnumerable<BundleEntry> entries = importer.Purge();

                store.Add(entries);
                index.Process(entries);
            }

        }

        public Bundle Transaction(Bundle bundle)
        {
            IEnumerable<BundleEntry> entries = importer.Import(bundle.Entries);
            
            try
            {
                store.Add(entries);
                index.Process(entries);

                exporter.RemoveBodyFromEntries(entries);
                bundle.Entries = entries.ToList();
                
                exporter.Externalize(bundle);
                return bundle;
            }
            catch
            {
                // todo: Purge batch from index 
                //store.PurgeBatch(transaction);
                throw;
            }
        }
        
        public Bundle History(DateTimeOffset? since, string sortby)
        {
            if (since == null) since = DateTimeOffset.MinValue;
            string title = String.Format("Full server-wide history for updates since {0}", since);
            RestUrl self = new RestUrl(this.Endpoint).AddPath(RestOperation.HISTORY);

            /*IEnumerable<BundleEntry> entries = store.ListVersions(since, Const.MAX_HISTORY_RESULT_SIZE);
            Snapshot snapshot = Snapshot.Create(title, self.Uri, null, entries, entries.Count());
            */
            
            IEnumerable<Uri> keys = store.History(since);
            Snapshot snapshot = Snapshot.Create(title, self.Uri, keys, sortby);
            
            Bundle bundle = pager.GetPage(snapshot, 0, Const.DEFAULT_PAGE_SIZE);
            exporter.Externalize(bundle);
            return bundle;
        }

        public Bundle History(string collection, DateTimeOffset? since, string sortby)
        {
            RequestValidator.ValidateCollectionName(collection);
            string title = String.Format("Full server-wide history for updates since {0}", since);
            RestUrl self = new RestUrl(this.Endpoint).AddPath(collection, RestOperation.HISTORY);

            IEnumerable<Uri> keys = store.History(collection, since);
           // Bundle bundle = BundleEntryFactory.CreateBundleWithEntries(title, self.Uri, Const.AUTHOR, Settings.AuthorUri, entries);
            
            var snapshot = Snapshot.Create(title, self.Uri, keys, sortby);
            store.AddSnapshot(snapshot);
            Bundle bundle = pager.GetPage(snapshot);
            exporter.Externalize(bundle);
            return bundle;
        }

        public Bundle History(string collection, string id, DateTimeOffset? since, string sortby)
        {
            Uri key = BuildKey(collection, id);

            if (!store.Exists(key))
                throw new SparkException(HttpStatusCode.NotFound, "There is no history because there is no {0} resource with id {1}.", collection, id);

            string title = String.Format("History for updates on '{0}' resource '{1}' since {2}", collection, id, since);
            RestUrl self = new RestUrl(this.Endpoint).AddPath(collection, id, RestOperation.HISTORY);

            IEnumerable<Uri> keys = store.History(key, since);
           
            Bundle bundle = pager.CreateSnapshotAndGetFirstPage(title, self.Uri, keys, sortby);
            exporter.Externalize(bundle);
            return bundle;
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
            binaryEntry.SelfLink = Key.GenerateCID();

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
            {
                result.Entries.Add(entry);
            }

            // Now add the newly constructed DocumentReference and the Binary
            result.Entries.Add(new ResourceEntry<DocumentReference>(Key.GenerateCID(), DateTimeOffset.Now, reference));
            result.Entries.Add(binaryEntry);

            // Process the constructed bundle as a Transaction and return the result
            return Transaction(result);
        }

        public TagList TagsFromServer()
        {
            IEnumerable<Tag> tags = tagstore.Tags();
            return new TagList(tags);
        }
        
        public TagList TagsFromResource(string resourcetype)
        {
            RequestValidator.ValidateCollectionName(resourcetype);
            IEnumerable<Tag> tags = tagstore.Tags(resourcetype);
            return new TagList(tags);
        }


        public TagList TagsFromInstance(string collection, string id)
        {
            Uri key = BuildKey(collection, id);
            BundleEntry entry = store.Get(key);

            if (entry == null)
                throwNotFound("Cannot retrieve tags because entry {0}/{1} does not exist", collection, id);

            return new TagList(entry.Tags);
         }


        public TagList TagsFromHistory(string collection, string id, string vid)
        {
            Uri key = BuildKey(collection, id, vid);
            BundleEntry entry = store.Get(key);

            if (entry == null)
                throwNotFound("Cannot retrieve tags because entry {0}/{1} does not exist", collection, id, vid); 
           
            else if (entry is DeletedEntry)
            {
                throw new SparkException(HttpStatusCode.Gone,
                    "A {0} resource with version {1} and id {2} exists, but it is a deletion (deleted on {3}).",
                    collection, vid, id, (entry as DeletedEntry).When);
            }

            return new TagList(entry.Tags);
        }

        public void AffixTags(string collection, string id, IEnumerable<Tag> tags)
        {
            Uri key = BuildKey(collection, id);
            if (tags == null) throw new SparkException("No tags specified on the request");

            BundleEntry entry = store.Get(key);
            if (entry == null)
                throw new SparkException(HttpStatusCode.NotFound, "Could not set tags. The resource was not found.");

            store.Add(entry);
        }

        public void AffixTags(string collection, string id, string vid, IEnumerable<Tag> tags)
        {
            Uri key = BuildKey(collection, id, vid);
            if (tags == null) throw new SparkException("No tags specified on the request");

            BundleEntry entry = store.Get(key);
            if (entry == null)
                throw new SparkException(HttpStatusCode.NotFound, "Could not set tags. The resource was not found.");

            entry.AffixTags(tags);
            store.Replace(entry);   
        }

        public void RemoveTags(string collection, string id, IEnumerable<Tag> tags)
        {
            if (tags == null) throw new SparkException("No tags specified on the request");

            Uri key = BuildKey(collection, id);
            BundleEntry entry = store.Get(key);
            if (entry == null)
                throw new SparkException(HttpStatusCode.NotFound, "Could not set tags. The resource was not found.");

            if (entry.Tags != null)
            {
                entry.Tags = entry.Tags.Exclude(tags).ToList();
            }
            
            store.Replace(entry);
        }

        public void RemoveTags(string collection, string id, string vid, IEnumerable<Tag> tags)
        {
            if (tags == null) throw new SparkException("Can not delete tags if no tags specified were specified");

            Uri key = BuildKey(collection, id, vid);

            ResourceEntry entry = (ResourceEntry)store.Get(key);
            if (entry == null)
                throw new SparkException(HttpStatusCode.NotFound, "Could not set tags. The resource was not found.");


            if (entry.Tags != null)
                entry.Tags = entry.Tags.Exclude(tags).ToList();

            store.Replace(entry);
        }

        public ResourceEntry<OperationOutcome> Validate(string collection, ResourceEntry entry, string id = null)
        {
            RequestValidator.ValidateCollectionName(collection);
            if(id != null) RequestValidator.ValidateId(id);

            if (entry == null) throw new SparkException("Validate needs a Resource in the body payload");

            entry.Title = "Validation test entity";
            entry.LastUpdated = DateTime.Now;
            entry.Id = id != null ? ResourceIdentity.Build(Endpoint, collection, id) : null;

            RequestValidator.ValidateResourceBody(entry, collection);
            var result = RequestValidator.ValidateEntry(entry);

            if (result != null)
                return new ResourceEntry<OperationOutcome>(new Uri("urn:guid:" + Guid.NewGuid()), DateTimeOffset.Now, result);
            else
                return null;
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

        public Bundle GetSnapshot(string snapshotkey, int index, int count)
        {
            Bundle bundle = pager.GetPage(snapshotkey, index, count);
            return bundle;
        }
    }
}