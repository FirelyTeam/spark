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
using Spark.Core;

using Hl7.Fhir.Validation;
//using Hl7.Fhir.Search;
using Hl7.Fhir.Serialization;

namespace Spark.Service
{
    // todo: ResourceImporter and resourceExporter are provisionally.

    public class FhirService 
    {
        //refac: private IFhirStore store;
        private IFhirStore store;
        private ISnapshotStore snapshotstore;

        //private IFhirIndex index;
        private IGenerator generator;
        //private ITagStore tagstore;
        private ResourceImporter importer = null;
        private ResourceExporter exporter = null;
        private Pager pager;
        public Uri Endpoint { get; private set; }

        public FhirService(Uri endpoint)
        {
            store = DependencyCoupler.Inject<IFhirStore>();
            //tagstore = DependencyCoupler.Inject<ITagStore>();
            snapshotstore = DependencyCoupler.Inject<ISnapshotStore>();
            generator = DependencyCoupler.Inject<IGenerator>();
            //index = DependencyCoupler.Inject<IFhirIndex>(); // Factory.Index;
            importer = DependencyCoupler.Inject<ResourceImporter>();
            exporter = DependencyCoupler.Inject<ResourceExporter>();
            pager = new Pager(store, snapshotstore, exporter);
            Endpoint = endpoint;
        }

        /*
        public Uri BuildKey(string collection, string id, string vid = null)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);
            if (vid != null) RequestValidator.ValidateVersionId(vid);
            Uri uri = ResourceIdentity.Build(collection, id, vid);
            return uri;
        }

        public Uri BuildLocation(string collection, string id, string vid = null)
        {
            RequestValidator.ValidateCollectionName(collection);
            RequestValidator.ValidateId(id);
            if (vid != null) RequestValidator.ValidateVersionId(vid);
            Uri uri = ResourceIdentity.Build(Endpoint, collection, id, vid);
            return uri;
        }
        */


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
        
        public Response Read(Key key)
        {
            Entry entry = store.Get(key);

            if (entry == null)
                return Respond.NotFound(key);

            else if (entry.Presense == Presense.Gone)
            {
                return Respond.Gone(entry);
            }

            // DSTU2: export
            // exporter.Externalize(result);

            return Respond.WithResource(entry);
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
        public Response VRead(Key key)
        {
            Entry entry = store.Get(key);

            if (entry == null)
                return Respond.NotFound(key);

            else if (entry.Presense == Presense.Gone)
            {
                return Respond.Gone(entry);
            }

            // DSTU2: export
            //exporter.Externalize(entry);
            return Respond.WithResource(entry);
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
        public Response Create(IKey key, Resource resource)
        {
            
            // DSTU2: import
            //RequestValidator.ValidateResourceBody(resource, key);
            //importer.AssertIdAllowed(id);           

            // In Dstu2 a user generated id is no longer possible, is it?
            //Uri location = BuildLocation(collection, id ?? generator.NextKey(entry.Resource));
         
            //entry.Id = location;

            
            // importer.Import(entry);
            if (!key.HasResourceId || key.IsForeign()) key = generator.NextKey(key);
            if (!key.HasVersionId) key = generator.NextHistoryKey(key);
            key.Apply(resource);
            Entry entry = new Entry(resource);

            store.Add(entry);
            
            // DSTU2: search
            //index.Process(entry);
            
            Entry result = store.Get(key);

            // DSTU2: export
            // exporter.Externalize(result);

            return Respond.WithEntry(HttpStatusCode.Created, result);
        }

        public bool Exists(Key key)
        {
            if (!key.IsLocal) return false;

            return store.Exists(key);
        }

        public Response Search(string collection, IEnumerable<Tuple<string, string>> parameters, int pageSize, string sortby)
        {
            // DSTU2: search
            /*
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
            

            exporter.Externalize(bundle);
            return bundle;
            */
            return Respond.WithError(HttpStatusCode.NotImplemented, "Search is not implemented yet");
        }

        
        public Response Update(IKey key, Resource resource)
        {
            RequestValidator.ValidateResourceBody(key, resource);
            Entry original = store.Get(key);

            if (original == null)
            {
                return Respond.WithError(HttpStatusCode.MethodNotAllowed, 
                    "Cannot update a resource {0} with id {1}, because it doesn't exist on this server",
                    key.TypeName, key.ResourceId);
            }
            // if the resource was deleted. It can be reinstated through an update.
            
            RequestValidator.ValidateVersion(resource, original.Resource);

            // Prepare the entry for storage
            // DSTU2: import
            //Entry updated = importer.Import(entry);
            //BundleEntry newentry = importer.Import(entry);
            //updated.Tags = current.Tags.Affix(entry.Tags).ToList();
            key = generator.NextHistoryKey(key);
            key.Apply(resource);
            Entry entry = new Entry(resource);
            store.Add(entry);

            
            //index.Process(updated);

            //exporter.Externalize(updated);

            return Respond.WithEntry(HttpStatusCode.OK, entry);
        }


        public Response Upsert(Key key, Resource resource)
        {
            if (this.Exists(key))
            {
                return this.Update(key, resource);
            }
            else
            {
                return this.Create(key, resource);
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
        public Response Delete(IKey key)
        {
            RequestValidator.ValidateKey(key, ValidateOptions.NotVersioned);
         
            Entry current = store.Get(key);
            if (current == null)
            {
                return Respond.NotFound(key);
            }
                    // "No {0} resource with id {1} was found, so it cannot be deleted.", collection, id);
            
            if (current.Presense == Presense.Present)
            {
                // Add a new deleted-entry to mark this entry as deleted
                //Entry deleted = importer.ImportDeleted(location);
                key = generator.NextHistoryKey(key);
                Entry deleted = Entry.Deleted(key, DateTimeOffset.UtcNow);
                
                store.Add(deleted);
                //index.Process(deleted);
                return Respond.WithCode(HttpStatusCode.NoContent);
                
            }
            else
            {
                return Respond.Gone(current);
            }
        }

        /*
        public void InjectKeys(Bundle bundle)
        {
            foreach (ResourceEntry entry in bundle.Entries.OfType<ResourceEntry>())
            {
                if (entry.Id == null)
                {
                    string collection = entry.GetResourceTypeName();
                    string id = generator.NextKey(entry.Resource);
                    Uri key = BuildKey(collection, id);
                    entry.Id = key;
                }
            }
        }
        */

        private void addHistoryKeys(List<Entry> entries)
        {
            // PERF: this needs a performance improvement!!!
            foreach(Entry entry in entries)
            {
                entry.Key = generator.NextHistoryKey(entry.Key);
            }
        }
        
        public Response Transaction(Bundle bundle)
        {
            List<Entry> entries = importer.Import(bundle);
            addHistoryKeys(entries);
            try
            {
                store.Add(entries);
                //index.Process(bundle);

                // DSTU2: export
                // exporter.RemoveBodyFromEntries(entries);
                bundle.Replace(entries);
                // exporter.Externalize(bundle);
                return Respond.WithResource(bundle);
            }
            catch
            {
                // todo: Purge batch from index 
                //store.PurgeBatch(transaction);
                throw;
            }
        }
        
        public Response History(DateTimeOffset? since, string sortby)
        {
            if (since == null) since = DateTimeOffset.MinValue;
            string title = String.Format("Full server-wide history for updates since {0}", since);
            RestUrl self = new RestUrl(this.Endpoint).AddPath(RestOperation.HISTORY);

            IEnumerable<string> keys = store.History(since);
            Snapshot snapshot = Snapshot.Create(title, self.Uri, keys, sortby);
            snapshotstore.AddSnapshot(snapshot);

            Bundle bundle = pager.GetPage(snapshot, 0, Const.DEFAULT_PAGE_SIZE);
            
            // DSTU2: export
            // exporter.Externalize(bundle);
            return Respond.WithResource(bundle);
        }

        public Response History(string collection, DateTimeOffset? since, string sortby)
        {
            RequestValidator.ValidateCollectionName(collection);
            string title = String.Format("Full server-wide history for updates since {0}", since); // todo: bad message when since=null
            RestUrl self = new RestUrl(this.Endpoint).AddPath(collection, RestOperation.HISTORY);

            IEnumerable<string> keys = store.History(collection, since);
            Snapshot snapshot = Snapshot.Create(title, self.Uri, keys, sortby);
            snapshotstore.AddSnapshot(snapshot);

            Bundle bundle = pager.GetPage(snapshot);
            // DSTU2: export
            // exporter.Externalize(bundle);
            return Respond.WithResource(bundle);
        }

        public Response History(Key key, DateTimeOffset? since, string sortby)
        {
            if (!store.Exists(key))
                return Respond.NotFound(key);
                 // throw new SparkException(HttpStatusCode.NotFound, "There is no history because there is no {0} resource with id {1}.", key.TypeName, key.ResourceId);

            string title = String.Format("History for updates on '{0}' resource '{1}' since {2}", key.TypeName, key.ResourceId, since);
            Uri self = key.ToUri(this.Endpoint);
                

            IEnumerable<string> keys = store.History(key, since);
            //Bundle bundle = pager.CreateSnapshotAndGetFirstPage(title, self, keys, sortby);
            Snapshot snapshot = Snapshot.Create(title, self, keys, sortby);
            snapshotstore.AddSnapshot(snapshot);

            Bundle bundle = pager.GetPage(snapshot);

            // DSTU2: export
            // exporter.Externalize(bundle);

            return Respond.WithResource(key, bundle);
        }

        
        public Response Mailbox(Bundle bundle, Binary body)
        {
            // DSTU2: mailbox
            /*
            // todo: this is not DSTU-1 conformant. 
            if(bundle == null || body == null) throw new SparkException("Mailbox requires a Bundle body payload"); 
            // For the connectathon, this *must* be a document bundle
            if (bundle.GetBundleType() != BundleType.Document)
                throw new SparkException("Mailbox endpoint currently only accepts Document feeds");

            Bundle result = new Bundle("Transaction result from posting of Document " + bundle.Id, DateTimeOffset.Now);

            // Build a binary with the original body content (=the unparsed Document)
            var binaryEntry = new ResourceEntry<Binary>(KeyHelper.NewCID(), DateTimeOffset.Now, body);
            binaryEntry.SelfLink = KeyHelper.NewCID();

            // Build a new DocumentReference based on the 1 composition in the bundle, referring to the binary
            var compositions = bundle.Entries.OfType<ResourceEntry<Composition>>();
            if (compositions.Count() != 1) throw new SparkException("Document feed should contain exactly 1 Composition resource");
            
            var composition = compositions.First().Resource;
            var reference = ConnectathonDocumentScenario.DocumentToDocumentReference(composition, bundle, body, binaryEntry.SelfLink);

            // Start by copying the original entries to the transaction, minus the Composition
            List<BundleEntry> entriesToInclude = new List<BundleEntry>();

            // todo: Only include stuff referenced by DocumentReference
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
            result.Entries.Add(new ResourceEntry<DocumentReference>(KeyHelper.NewCID(), DateTimeOffset.Now, reference));
            result.Entries.Add(binaryEntry);

            // Process the constructed bundle as a Transaction and return the result
            return Transaction(result);
            */
            return Respond.WithError(HttpStatusCode.NotImplemented);
        }

        /*
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
            if (tags == null) throw new SparkException("No tags specified on the request");
            Uri key = BuildKey(collection, id);
            BundleEntry entry = store.Get(key);
            
            if (entry == null)
                throw new SparkException(HttpStatusCode.NotFound, "Could not set tags. The resource was not found.");

            entry.AffixTags(tags);
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
        */

        public OperationOutcome Validate(Resource resource)
        {
            var outcome = RequestValidator.ValidateResource(resource);
            return outcome;
        }

        public Response Validate(Key key, Resource resource)
        {
            if (resource == null) throw new SparkException("Validate needs a Resource in the body payload");
            //if (entry.Resource == null) throw new SparkException("Validate needs a Resource in the body payload");

            //  DSTU2: validation
            // entry.Resource.Title = "Validation test entity";
            // entry.LastUpdated = DateTime.Now;
            // entry.Id = id != null ? ResourceIdentity.Build(Endpoint, collection, id) : null;

            RequestValidator.ValidateResourceBody(key, resource);
            
            // DSTU2: validation
            var outcome = RequestValidator.ValidateResource(resource);
            
            if (outcome == null)
                return Respond.WithCode(HttpStatusCode.OK);
            else
                return Respond.WithResource(422, outcome);
        }
       
        public Response Conformance()
        {
            var conformance = DependencyCoupler.Inject<Conformance>();
            return Respond.WithResource(conformance);

            // DSTU2: conformance
            //var conformance = ConformanceBuilder.Build();

            //return Respond.WithResource(conformance);

            //var entry = new ResourceEntry<Conformance>(KeyHelper.NewCID(), DateTimeOffset.Now, conformance);
            //return entry;

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

        public Response GetSnapshot(string snapshotkey, int index, int count)
        {
            Bundle bundle = pager.GetPage(snapshotkey, index, count);
            return Respond.WithResource(bundle);
        }
    }
}