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
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Auxiliary;
using Spark.Engine.Interfaces;
using Spark.Engine.Logging;
using Spark.Engine.Service;

namespace Spark.Service
{

    public class FhirService
    {
        protected IFhirStore fhirStore;
        protected ISnapshotStore snapshotstore;
        protected IFhirIndex fhirIndex;

        protected IGenerator keyGenerator;
        protected ILocalhost localhost;
        protected IServiceListener serviceListener;
        private readonly IFhirResponseFactory responseFactory;

        protected Transfer transfer;
        protected Pager pager;

        protected IndexService _indexService;

        private SparkEngineEventSource _log = SparkEngineEventSource.Log;

        public FhirService(ILocalhost localhost, IFhirStore fhirStore, ISnapshotStore snapshotStore, IGenerator keyGenerator,
            IFhirIndex fhirIndex, IServiceListener serviceListener, IFhirResponseFactory responseFactory, IndexService indexService)
        {
            this.localhost = localhost;
            this.fhirStore = fhirStore;
            this.snapshotstore = snapshotStore;
            this.keyGenerator = keyGenerator;
            this.fhirIndex = fhirIndex;
            this.serviceListener = serviceListener;
            this.responseFactory = responseFactory;
            _indexService = indexService;

            transfer = new Transfer(this.keyGenerator, localhost);
            pager = new Pager(this.fhirStore, snapshotstore, localhost, transfer, ModelInfo.SearchParameters);
            //TODO: Use FhirModel instead of ModelInfo for the searchparameters.
        }

        public FhirResponse Read(Key key, ConditionalHeaderParameters parameters = null)
        {
            _log.ServiceMethodCalled("read");

            ValidateKey(key);

            return responseFactory.GetFhirResponse(key, parameters);
        }

        public FhirResponse ReadMeta(Key key)
        {
            _log.ServiceMethodCalled("readmeta");

            ValidateKey(key);

            Entry entry = fhirStore.Get(key);

            if (entry == null)
            {
                return Respond.NotFound(key);
            }
            else if (entry.IsDeleted())
            {
                return Respond.Gone(entry);
            }

            return Respond.WithMeta(entry);
        }

        private static void ValidateKey(Key key, bool includeVersion = false)
        {
            Validate.HasTypeName(key);
            Validate.HasResourceId(key);
            if (includeVersion)
            {
                Validate.HasVersion(key);
            }
            else
            {
                Validate.HasNoVersion(key);
            }
            Validate.Key(key);
        }

        public FhirResponse AddMeta(Key key, Parameters parameters)
        {
            Entry entry = fhirStore.Get(key);

            if (entry == null)
            {
                return Respond.NotFound(key);
            }
            else if (entry.IsDeleted())
            {
                return Respond.Gone(entry);
            }

            entry.Resource.AffixTags(parameters);
            Store(entry);

            return Respond.WithMeta(entry);
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
        public FhirResponse VersionRead(Key key)
        {
            _log.ServiceMethodCalled("versionread");

            ValidateKey(key, true);

            return responseFactory.GetFhirResponse(key);
        }

        /// <summary>
        /// Create a new resource with a server assigned id.
        /// </summary>
        /// <param name="collection">The resource type, in lowercase</param>
        /// <param name="resource">The data for the Resource to be created</param>
        /// <returns>
        /// Returns 
        ///     201 Created - on successful creation
        /// </returns>
        public FhirResponse Create(IKey key, Resource resource)
        {
            Validate.Key(key);
            Validate.ResourceType(key, resource);
            Validate.HasTypeName(key);
            Validate.HasNoResourceId(key);
            Validate.HasNoVersion(key);

            Entry entry = Entry.POST(key, resource);
            transfer.Internalize(entry);

            Store(entry);

            // API: The api demands a body. This is wrong
            //CCR: The documentations specifies that servers should honor the Http return preference header
            Entry result = fhirStore.Get(entry.Key);
            transfer.Externalize(result);
            return Respond.WithResource(HttpStatusCode.Created, result);
        }

        public FhirResponse Put(IKey key, Resource resource)
        {
            Validate.Key(key);
            Validate.ResourceType(key, resource);
            Validate.HasTypeName(key);
            Validate.HasResourceId(key);
            Validate.HasResourceId(resource);
            Validate.IsResourceIdEqual(key, resource);

            Entry current = fhirStore.Get(key);

            Entry entry = Entry.PUT(key, resource);
            transfer.Internalize(entry);


            Store(entry);

            // API: The api demands a body. This is wrong
            //CCR: The documentations specifies that servers should honor the Http return preference header
            Entry result = fhirStore.Get(entry.Key);
            transfer.Externalize(result);

            return Respond.WithResource(current != null ? HttpStatusCode.OK : HttpStatusCode.Created, result);
        }

        public FhirResponse ConditionalCreate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> query)
        {
            // DSTU2: search
            throw new NotImplementedException("This will be implemented after search is DSTU2");
        }

        public FhirResponse Search(string type, SearchParams searchCommand)
        {
            _log.ServiceMethodCalled("search");

            Validate.TypeName(type);
            SearchResults results = fhirIndex.Search(type, searchCommand);

            if (results.HasErrors)
            {
                throw new SparkException(HttpStatusCode.BadRequest, results.Outcome);
            }

            UriBuilder builder = new UriBuilder(localhost.Uri(type));
            builder.Query = results.UsedParameters;
            Uri link = builder.Uri;

            var snapshot = pager.CreateSnapshot(link, results, searchCommand);
            Bundle bundle = pager.GetFirstPage(snapshot);

            if (results.HasIssues)
            {
                bundle.AddResourceEntry(results.Outcome, new Uri("outcome/1", UriKind.Relative).ToString());
            }

            return Respond.WithBundle(bundle);
        }

        //public FhirResponse Search(string type, IEnumerable<Tuple<string, string>> parameters, int pageSize, string sortby)
        //{
        //    Validate.TypeName(type);
        //    Uri link = localhost.Uri(type);

        //    IEnumerable<string> keys = store.List(type);
        //    var snapshot = pager.CreateSnapshot(Bundle.BundleType.Searchset, link, keys, );
        //    Bundle bundle = pager.GetFirstPage(snapshot);
        //    return Respond.WithBundle(bundle, localhost.Base);
        // DSTU2: search
        /*
        Query query = FhirParser.ParseQueryFromUriParameters(collection, parameters);
        ICollection<string> includes = query.Includes;

        SearchResults results = index.Search(query);

        if (results.HasErrors)
        {
            throw new SparkException(HttpStatusCode.BadRequest, results.Outcome);
        }

        Uri link = localhost.Uri(type).AddPath(results.UsedParameters);

        Bundle bundle = pager.GetFirstPage(link, keys, sortby);

        /*
        if (results.HasIssues)
        {
            var outcomeEntry = BundleEntryFactory.CreateFromResource(results.Outcome, new Uri("outcome/1", UriKind.Relative), DateTimeOffset.Now);
            outcomeEntry.SelfLink = outcomeEntry.Id;
            bundle.Entries.Add(outcomeEntry);
        }
        return Respond.WithBundle(bundle);
        */
        //}

        //public FhirResponse Update(IKey key, Resource resource)
        //{
        //    Validate.HasTypeName(key);
        //    Validate.HasNoVersion(key);
        //    Validate.ResourceType(key, resource);

        //    Interaction original = store.Get(key);

        //    if (original == null)
        //    {
        //        return Respond.WithError(HttpStatusCode.MethodNotAllowed,
        //            "Cannot update resource {0}/{1}, because it doesn't exist on this server",
        //            key.TypeName, key.ResourceId);
        //    }   

        //    Interaction interaction = Interaction.PUT(key, resource);
        //    interaction.Resource.AffixTags(original.Resource);

        //    transfer.Internalize(interaction);
        //    Store(interaction);

        //    // todo: does this require a response?
        //    transfer.Externalize(interaction);
        //    return Respond.WithEntry(HttpStatusCode.OK, interaction);
        //}

        public FhirResponse VersionSpecificUpdate(IKey versionedkey, Resource resource)
        {
            Validate.HasTypeName(versionedkey);
            Validate.HasVersion(versionedkey);

            Key key = versionedkey.WithoutVersion();
            Entry current = fhirStore.Get(key);
            Validate.IsSameVersion(current.Key, versionedkey);

            return this.Put(key, resource);
        }

        /// <summary>
        /// Updates a resource if it exist on the given id, or creates the resource if it is new.
        /// If a VersionId is included a version specific update will be attempted.
        /// </summary>
        /// <returns>200 OK (on success)</returns>
        public FhirResponse Update(IKey key, Resource resource)
        {
            if (key.HasVersionId())
            {
                return this.VersionSpecificUpdate(key, resource);
            }
            else
            {
                return this.Put(key, resource);
            }
        }

        public FhirResponse ConditionalUpdate(Key key, Resource resource, SearchParams _params)
        {
            Key existing = fhirIndex.FindSingle(key.TypeName, _params).WithoutVersion();
            return this.Update(existing, resource);
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

            Entry current = fhirStore.Get(key);

            if (current != null && current.IsPresent)
            {
                // Add a new deleted-entry to mark this entry as deleted
                //Entry deleted = importer.ImportDeleted(location);
                key = keyGenerator.NextHistoryKey(key);
                Entry deleted = Entry.DELETE(key, DateTimeOffset.UtcNow);

                Store(deleted);
            }
            return Respond.WithCode(HttpStatusCode.NoContent);
        }

        public FhirResponse ConditionalDelete(Key key, IEnumerable<Tuple<string, string>> parameters)
        {
            // DSTU2: transaction
            throw new NotImplementedException("This will be implemented after search in DSTU2");
            // searcher.search(parameters)
            // assert count = 1
            // get result id

            //string id = "to-implement";

            //key.ResourceId = id;
            //Interaction deleted = Interaction.DELETE(key, DateTimeOffset.UtcNow);
            //store.Add(deleted);
            //return Respond.WithCode(HttpStatusCode.NoContent);
        }

        public FhirResponse HandleInteraction(Entry interaction)
        {
            switch (interaction.Method)
            {
                case Bundle.HTTPVerb.PUT: return this.Update(interaction.Key, interaction.Resource);
                case Bundle.HTTPVerb.POST: return this.Create(interaction.Key, interaction.Resource);
                case Bundle.HTTPVerb.DELETE: return this.Delete(interaction.Key);
                default: return Respond.Success;
            }
        }

        // These should eventually be Interaction! = FhirRequests. Not Entries.
        public FhirResponse Transaction(IList<Entry> interactions)
        {
            transfer.Internalize(interactions);

            var resources = new List<Resource>();

            foreach (Entry interaction in interactions)
            {
                FhirResponse response = HandleInteraction(interaction);

                if (!response.IsValid) return response;
                resources.Add(response.Resource);
            }

            transfer.Externalize(interactions);

            Bundle bundle = localhost.CreateBundle(Bundle.BundleType.TransactionResponse).Append(interactions);

            return Respond.WithBundle(bundle);
        }

        public FhirResponse Transaction(Bundle bundle)
        {
            var interactions = localhost.GetEntries(bundle);
            transfer.Internalize(interactions);

            fhirStore.Add(interactions);
            fhirIndex.Process(interactions);
            transfer.Externalize(interactions);

            bundle = localhost.CreateBundle(Bundle.BundleType.TransactionResponse).Append(interactions);

            return Respond.WithBundle(bundle);
        }

        public FhirResponse History(HistoryParameters parameters)
        {
            var since = parameters.Since ?? DateTimeOffset.MinValue;
            Uri link = localhost.Uri(RestOperation.HISTORY);

            IEnumerable<string> keys = fhirStore.History(since);
            var snapshot = pager.CreateSnapshot(Bundle.BundleType.History, link, keys, parameters.SortBy, parameters.Count, null);
            Bundle bundle = pager.GetFirstPage(snapshot);

            // DSTU2: export
            // exporter.Externalize(bundle);
            return Respond.WithBundle(bundle);
        }

        public FhirResponse History(string type, HistoryParameters parameters)
        {
            Validate.TypeName(type);
            Uri link = localhost.Uri(type, RestOperation.HISTORY);

            IEnumerable<string> keys = fhirStore.History(type, parameters.Since);
            var snapshot = pager.CreateSnapshot(Bundle.BundleType.History, link, keys, parameters.SortBy, parameters.Count, null);
            Bundle bundle = pager.GetFirstPage(snapshot);

            return Respond.WithResource(bundle);
        }

        public FhirResponse History(Key key, HistoryParameters parameters)
        {
            if (!fhirStore.Exists(key))
            {
                return Respond.NotFound(key);
            }

            Uri link = localhost.Uri(key);

            IEnumerable<string> keys = fhirStore.History(key, parameters.Since);
            var snapshot = pager.CreateSnapshot(Bundle.BundleType.History, link, keys, parameters.SortBy, parameters.Count);
            Bundle bundle = pager.GetFirstPage(snapshot);

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
            if (resource == null) throw Error.BadRequest("Validate needs a Resource in the body payload");
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

        public FhirResponse GetPage(string snapshotkey, int index)
        {
            Bundle bundle = pager.GetPage(snapshotkey, index);
            return Respond.WithBundle(bundle);
        }

        private void Store(Entry entry)
        {
            fhirStore.Add(entry);

            //CK: try the new indexing service.
            if (_indexService != null)
            {
                _indexService.Process(entry);
            }

            else if (fhirIndex != null)
            {
                //TODO: If IndexService is working correctly, remove the reference to fhirIndex.
                fhirIndex.Process(entry);
            }


            if (serviceListener != null)
            {
                Uri location = localhost.GetAbsoluteUri(entry.Key);
                // todo: what we want is not to send localhost to the listener, but to add the Resource.Base. But that is not an option in the current infrastructure.
                // It would modify interaction.Resource, while 
                serviceListener.Inform(location, entry);
            }
        }

    }
}