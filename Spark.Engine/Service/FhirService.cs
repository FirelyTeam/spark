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
using Spark.Core.Auxiliary;

namespace Spark.Service
{

    public class FhirService 
    {
        private IFhirStore store;
        private ISnapshotStore snapshotstore;
        
        private IGenerator generator;
        //private ITagStore tagstore;
        private ResourceImporter importer = null;
        private ResourceExporter exporter = null;
        private Pager pager;
        public Uri Endpoint { get; private set; }

        public FhirService(Uri endpoint)
        {
            store = DependencyCoupler.Inject<IFhirStore>();
            snapshotstore = DependencyCoupler.Inject<ISnapshotStore>();
            generator = DependencyCoupler.Inject<IGenerator>();
            importer = DependencyCoupler.Inject<ResourceImporter>();
            exporter = DependencyCoupler.Inject<ResourceExporter>();

            pager = new Pager(store, snapshotstore, exporter);
            Endpoint = endpoint;
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
        public FhirResponse Read(Key key)
        {
            Interaction interaction = store.Get(key);

            if (interaction == null)
                return Respond.NotFound(key);

            else if (interaction.IsDeleted())
            {
                return Respond.Gone(interaction);
            }

            // DSTU2: export
            // exporter.Externalize(result);

            return Respond.WithResource(interaction);
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
        public FhirResponse VRead(Key key)
        {
            Interaction interaction = store.Get(key);

            if (interaction == null)
                return Respond.NotFound(key);

            else if (interaction.IsDeleted())
            {
                return Respond.Gone(interaction);
            }

            // DSTU2: export
            //exporter.Externalize(entry);
            return Respond.WithResource(interaction);
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
        public FhirResponse Create(IKey key, Resource resource)
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
            key.ApplyTo(resource);
            Interaction entry = new Interaction(resource);

            store.Add(entry);
            
            // DSTU2: search
            //index.Process(entry);
            
            Interaction result = store.Get(key);

            // DSTU2: export
            // exporter.Externalize(result);

            return Respond.WithEntry(HttpStatusCode.Created, result);
        }

        public FhirResponse ConditionalCreate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> query)
        {
            // DSTU2: search
            throw new NotImplementedException("This will be implemented after search is DSTU2");
        }

        public FhirResponse Search(string collection, IEnumerable<Tuple<string, string>> parameters, int pageSize, string sortby)
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
        
        public FhirResponse Update(IKey key, Resource resource)
        {
            Validate.ResourceType(key, resource);

            Interaction original = store.Get(key);

            if (original == null)
            {
                return Respond.WithError(HttpStatusCode.MethodNotAllowed, 
                    "Cannot update a resource {0} with id {1}, because it doesn't exist on this server", 
                    key.TypeName, key.ResourceId);
            }
            // if the resource was deleted. It can be reinstated through an update.
            
            Validate.SameVersion(resource, original.Resource);

            // Prepare the entry for storage
            // DSTU2: import
            //Entry updated = importer.Import(entry);
            //BundleEntry newentry = importer.Import(entry);
            //updated.Tags = current.Tags.Affix(entry.Tags).ToList();
            key = generator.NextHistoryKey(key);
            key.ApplyTo(resource);
            Interaction entry = new Interaction(resource);
            store.Add(entry);

            
            //index.Process(updated);

            //exporter.Externalize(updated);

            return Respond.WithEntry(HttpStatusCode.OK, entry);
        }

        public FhirResponse VersionSpecificUpdate(Key key, Resource resource)
        {
            Interaction current = store.Get(key.WithoutVersion());
            if (current.Key.VersionId == key.VersionId)
            {
                return this.Update(key, resource);
            }
            else
            {
                return Respond.WithError(HttpStatusCode.PreconditionFailed);
            }
        }

        public FhirResponse Upsert(Key key, Resource resource)
        {
            if (key.IsForeign())
            {
                return this.Create(key, resource);
            }
            else if (key.HasVersionId)
            {
                return this.VersionSpecificUpdate(key, resource);
            }
            else if (store.Exists(key))
            {
                return this.Update(key, resource);
            }
            else
            {
                return this.Create(key, resource);
            }
        }

        public FhirResponse ConditionalUpdate(Key key, Resource resource, IEnumerable<Tuple<string, string>> query)
        {
            // DSTU2: search
            throw new NotImplementedException("This will be implemented after search is at DSTU2");
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
        public FhirResponse Delete(IKey key)
        {
            Validate.Key(key);
            Validate.HasNoVersion(key);
         
            Interaction current = store.Get(key);
            if (current == null)
            {
                return Respond.NotFound(key);
            }
                    // "No {0} resource with id {1} was found, so it cannot be deleted.", collection, id);
            
            if (current.IsPresent)
            {
                // Add a new deleted-entry to mark this entry as deleted
                //Entry deleted = importer.ImportDeleted(location);
                key = generator.NextHistoryKey(key);
                Interaction deleted = Interaction.CreateDeleted(key, DateTimeOffset.UtcNow);
                
                store.Add(deleted);
                //index.Process(deleted);
                return Respond.WithCode(HttpStatusCode.NoContent);
                
            }
            else
            {
                return Respond.Gone(current);
            }
        }

        public FhirResponse ConditionalDelete(Key key, IEnumerable<Tuple<string, string>> parameters)
        {
            // DSTU2: transaction
            throw new NotImplementedException("This will be implemented after search is DSTU2");
            // searcher.search(parameters)
            // assert count = 1
            // get result id
            string id = "to-implement";
            key.ResourceId = id;
            Interaction deleted = Interaction.CreateDeleted(key, DateTimeOffset.UtcNow);
            store.Add(deleted);
            return Respond.WithCode(HttpStatusCode.NoContent);
        }

        public FhirResponse Transaction(Bundle bundle)
        {
            List<Interaction> entries = importer.Import(bundle);
            generator.AddHistoryKeys(entries);
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
                // DSTU2: transaction
                //index.Rollback
                
                throw;
            }
        }
        
        public FhirResponse History(DateTimeOffset? since, string sortby)
        {
            if (since == null) since = DateTimeOffset.MinValue;
            string title = String.Format("Full server-wide history for updates since {0}", Language.Since(since));
            RestUrl self = new RestUrl(this.Endpoint).AddPath(RestOperation.HISTORY);

            IEnumerable<string> keys = store.History(since);
            Snapshot snapshot = Snapshot.Create(title, self.Uri, keys, sortby);
            snapshotstore.AddSnapshot(snapshot);

            Bundle bundle = pager.GetPage(snapshot, 0, Const.DEFAULT_PAGE_SIZE);
            
            // DSTU2: export
            // exporter.Externalize(bundle);
            return Respond.WithResource(bundle);
        }

        public FhirResponse History(string type, DateTimeOffset? since, string sortby)
        {
            Validate.TypeName(type);
            string title = String.Format("Full server-wide history for updates since {0}", Language.Since(since)); 
            RestUrl self = new RestUrl(this.Endpoint).AddPath(type, RestOperation.HISTORY);

            IEnumerable<string> keys = store.History(type, since);
            Snapshot snapshot = Snapshot.Create(title, self.Uri, keys, sortby);
            snapshotstore.AddSnapshot(snapshot);

            Bundle bundle = pager.GetPage(snapshot);
            // DSTU2: export
            // exporter.Externalize(bundle);
            return Respond.WithResource(bundle);
        }

        public FhirResponse History(Key key, DateTimeOffset? since, string sortby)
        {
            if (!store.Exists(key))
                return Respond.NotFound(key);
                 // throw new SparkException(HttpStatusCode.NotFound, "There is no history because there is no {0} resource with id {1}.", key.TypeName, key.ResourceId);

            string title = String.Format("History for updates on '{0}' resource '{1}' since {2}", key.TypeName, key.ResourceId, Language.Since(since));
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
        
        public FhirResponse Mailbox(Bundle bundle, Binary body)
        {
            // DSTU2: mailbox
            /*
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

        public FhirResponse ValidateOperation(Key key, Resource resource)
        {
            if (resource == null) throw new SparkException("Validate needs a Resource in the body payload");
            //if (entry.Resource == null) throw new SparkException("Validate needs a Resource in the body payload");

            //  DSTU2: validation
            // entry.Resource.Title = "Validation test entity";
            // entry.LastUpdated = DateTime.Now;
            // entry.Id = id != null ? ResourceIdentity.Build(Endpoint, collection, id) : null;

            Validate.ResourceType(key, resource);
            
            // DSTU2: validation
            var outcome = Validate.AgainstSchema(resource);
            
            if (outcome == null)
                return Respond.WithCode(HttpStatusCode.OK);
            else
                return Respond.WithResource(422, outcome);
        }
       
        public FhirResponse Conformance()
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

        public FhirResponse GetSnapshot(string snapshotkey, int index, int count)
        {
            Bundle bundle = pager.GetPage(snapshotkey, index, count);
            return Respond.WithResource(bundle);
        }

    }
}