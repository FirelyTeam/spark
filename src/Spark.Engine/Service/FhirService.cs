using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Storage;
using Spark.Service;

namespace Spark.Engine.Service
{
    public class FhirService : ExtendableWith<IFhirServiceExtension>, IFhirService, IInteractionHandler 
        //CCCR: FhirService now implementents InteractionHandler that is used by the TransactionService to actually perform the operation. 
        //This creates a circular reference that is solved by sending the handler on each call. 
        //A future step might be to split that part into a different service (maybe StorageService?)
    {
        private readonly IFhirResponseFactory responseFactory;
        private readonly ITransfer transfer;
        private readonly ICompositeServiceListener serviceListener;
        public FhirService(IFhirServiceExtension[] extensions, 
            IFhirResponseFactory responseFactory, //TODO: can we remove this dependency?
            ITransfer transfer,
            ICompositeServiceListener serviceListener = null) //TODO: can we remove this dependency? - CCR
        {
            this.responseFactory = responseFactory;
            this.transfer = transfer;
            this.serviceListener = serviceListener;

            foreach (IFhirServiceExtension serviceExtension in extensions)
            {
                this.AddExtension(serviceExtension);
            }
        }

        public FhirResponse Read(IKey key, ConditionalHeaderParameters parameters = null)
        {
            ValidateKey(key);

            Entry entry = GetFeature<IResourceStorageService>().Get(key);

            return responseFactory.GetFhirResponse(entry, key, parameters);
        }

        public FhirResponse ReadMeta(IKey key)
        {
            ValidateKey(key);

            Entry entry = GetFeature<IResourceStorageService>().Get(key);

            return responseFactory.GetMetadataResponse(entry, key);
        }

        public FhirResponse AddMeta(IKey key, Parameters parameters)
        {
            var storageService = GetFeature<IResourceStorageService>();
            Entry entry = storageService.Get(key);

            if (entry != null && entry.IsDeleted() == false)
            {
                entry.Resource.AffixTags(parameters);
                storageService.Add(entry);
            }

            return responseFactory.GetMetadataResponse(entry, key);
        }

        public FhirResponse VersionRead(IKey key)
        {
            ValidateKey(key, true);
            Entry entry = GetFeature<IResourceStorageService>().Get(key);

            return responseFactory.GetFhirResponse(entry, key);
        }

        public FhirResponse Create(IKey key, Resource resource)
        {
            Validate.Key(key);
            Validate.HasTypeName(key);
            Validate.ResourceType(key, resource);

            Validate.HasNoResourceId(key);
            Validate.HasNoVersion(key);


            Entry result = Store(Entry.POST(key, resource));

            return Respond.WithResource(HttpStatusCode.Created, result);
        }

        public FhirResponse Create(Entry entry)
        {
            Validate.Key(entry.Key);
            Validate.HasTypeName(entry.Key);
            Validate.ResourceType(entry.Key, entry.Resource);

            if (entry.State != EntryState.Internal)
            {
                Validate.HasNoResourceId(entry.Key);
                Validate.HasNoVersion(entry.Key);
            }


            Entry result = Store(entry);

            return Respond.WithResource(HttpStatusCode.Created, result);
        }

        public FhirResponse Put(Entry entry)
        {
            Validate.Key(entry.Key);
            Validate.ResourceType(entry.Key, entry.Resource);
            Validate.HasTypeName(entry.Key);
            Validate.HasResourceId(entry.Key);
           

            var storageService = GetFeature<IResourceStorageService>();
            Entry current = storageService.Get(entry.Key.WithoutVersion());

            Entry result = Store(entry);

            return Respond.WithResource(current != null ? HttpStatusCode.OK : HttpStatusCode.Created, result);

        }
        public FhirResponse Put(IKey key, Resource resource)
        {
            Validate.HasResourceId(resource);
            Validate.IsResourceIdEqual(key, resource);
            return Put(Entry.PUT(key, resource));
        }

        public FhirResponse ConditionalCreate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> parameters)
        {
            return ConditionalCreate(key, resource, SearchParams.FromUriParamList(parameters));
        }

        public FhirResponse ConditionalCreate(IKey key, Resource resource, SearchParams parameters)
        {
            ISearchService searchStore = this.FindExtension<ISearchService>();
            ITransactionService transactionService = this.FindExtension<ITransactionService>();
            if (searchStore == null || transactionService == null)
                throw new NotSupportedException("Operation not supported");

            return transactionService.HandleTransaction(
                    ResourceManipulationOperationFactory.CreatePost(resource, key, searchStore, parameters),
                    this);
        }

        public FhirResponse Everything(IKey key)
        {
            ISearchService searchService = this.GetFeature<ISearchService>();

            Snapshot snapshot = searchService.GetSnapshotForEverything(key);

            return CreateSnapshotResponse(snapshot);
        }

        public FhirResponse Document(IKey key)
        {
            Validate.HasResourceType(key, ResourceType.Composition);

            var searchCommand = new SearchParams();
            searchCommand.Add("_id", key.ResourceId);
            var includes = new List<string>()
            {
                "Composition:subject"
                , "Composition:author"
                , "Composition:attester" //Composition.attester.party
                , "Composition:custodian"
                , "Composition:eventdetail" //Composition.event.detail
                , "Composition:encounter"
                , "Composition:entry" //Composition.section.entry
            };
            foreach (var inc in includes)
            {
                searchCommand.Include.Add(inc);
            }
            return Search(key.TypeName, searchCommand);
        }

        public FhirResponse VersionSpecificUpdate(IKey versionedkey, Resource resource)
        {
            Validate.HasTypeName(versionedkey);
            Validate.HasVersion(versionedkey);
            Key key = versionedkey.WithoutVersion();
            Entry current = GetFeature<IResourceStorageService>().Get(key);
            Validate.IsSameVersion(current.Key, versionedkey);

            return this.Put(key, resource);
        }

        public FhirResponse Update(IKey key, Resource resource)
        {
            return key.HasVersionId() ? this.VersionSpecificUpdate(key, resource)
                : this.Put(key, resource);
        }

        public FhirResponse ConditionalUpdate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> parameters)
        {
            return ConditionalUpdate(key, resource, SearchParams.FromUriParamList(parameters));
        }

        public FhirResponse ConditionalUpdate(IKey key, Resource resource, SearchParams _params)
        {
            //if update receives a key with no version how do we handle concurrency?
            ISearchService searchStore = this.FindExtension<ISearchService>();
            ITransactionService transactionService = this.FindExtension<ITransactionService>();
            if (searchStore == null || transactionService == null)
                throw new NotSupportedException("Operation not supported");
            return transactionService.HandleTransaction(
                ResourceManipulationOperationFactory.CreatePut(resource, key, searchStore, _params),
                this);
        }

        public FhirResponse Delete(IKey key)
        {
            Validate.Key(key);
            Validate.HasNoVersion(key);

            var resourceStorage = GetFeature<IResourceStorageService>();

            Entry current = resourceStorage.Get(key);
            if (current != null && current.IsPresent)
            {
                return Delete(Entry.DELETE(key, DateTimeOffset.UtcNow));
            }
            return Respond.WithCode(HttpStatusCode.NoContent);

        }

        public FhirResponse Delete(Entry entry)
        {
            Validate.Key(entry.Key);
            Store(entry);
            return Respond.WithCode(HttpStatusCode.NoContent);
        }

        public FhirResponse ConditionalDelete(IKey key, IEnumerable<Tuple<string, string>> parameters)
        {
            return ConditionalDelete(key, SearchParams.FromUriParamList(parameters));
        }

        public FhirResponse ConditionalDelete(IKey key, SearchParams _params)
        {
            ISearchService searchStore = this.FindExtension<ISearchService>();
            ITransactionService transactionService = this.FindExtension<ITransactionService>();
            if (searchStore == null || transactionService == null)
                throw new NotSupportedException("Operation not supported");

            return transactionService.HandleTransaction(ResourceManipulationOperationFactory.CreateDelete(key, searchStore, _params),
                this)??  Respond.WithCode(HttpStatusCode.NotFound);
        }

        public FhirResponse ValidateOperation(IKey key, Resource resource)
        {
            if (resource == null) throw Error.BadRequest("Validate needs a Resource in the body payload");
            Validate.ResourceType(key, resource);

            // DSTU2: validation
            var outcome = Validate.AgainstSchema(resource);

            if (outcome == null)
                return Respond.WithCode(HttpStatusCode.OK);
            else
                return Respond.WithResource(422, outcome);
        }

        public FhirResponse Search(string type, SearchParams searchCommand, int pageIndex = 0)
        {
            ISearchService searchService = this.GetFeature<ISearchService>();

            Snapshot snapshot = searchService.GetSnapshot(type, searchCommand);

            return CreateSnapshotResponse(snapshot, pageIndex);
        }

        private FhirResponse CreateSnapshotResponse(Snapshot snapshot, int pageIndex = 0)
        {
            IPagingService pagingExtension = this.FindExtension<IPagingService>();
            IResourceStorageService resourceStorage = this.FindExtension<IResourceStorageService>();
            if (pagingExtension == null)
            {
                Bundle bundle = new Bundle()
                {
                    Type = snapshot.Type,
                    Total = snapshot.Count
                };
                bundle.Append(resourceStorage.Get(snapshot.Keys));
                return responseFactory.GetFhirResponse(bundle);
            }
            else
            {
                Bundle bundle = pagingExtension.StartPagination(snapshot).GetPage(pageIndex);
                return responseFactory.GetFhirResponse(bundle);
            }
        }

        public FhirResponse Transaction(IList<Entry> interactions)
        {
            ITransactionService transactionExtension = this.GetFeature<ITransactionService>();
            return responseFactory.GetFhirResponse(
                transactionExtension.HandleTransaction(interactions, this), 
                Bundle.BundleType.TransactionResponse);
        }

        public FhirResponse Transaction(Bundle bundle)
        {
            ITransactionService transactionExtension = this.GetFeature<ITransactionService>();
            return responseFactory.GetFhirResponse(
                transactionExtension.HandleTransaction(bundle, this), 
                Bundle.BundleType.TransactionResponse);
        }

        public FhirResponse History(HistoryParameters parameters)
        {
            IHistoryService historyExtension = this.GetFeature<IHistoryService>();
        
            return CreateSnapshotResponse(historyExtension.History(parameters));
        }

        public FhirResponse History(string type, HistoryParameters parameters)
        {
            IHistoryService historyExtension = this.GetFeature<IHistoryService>();

            return CreateSnapshotResponse(historyExtension.History(type, parameters));
        }

        public FhirResponse History(IKey key, HistoryParameters parameters)
        {
            IResourceStorageService storageService = GetFeature<IResourceStorageService>();
            if (storageService.Get(key) == null)
            {
                return Respond.NotFound(key);
            }
            IHistoryService historyExtension = this.GetFeature<IHistoryService>();

            return CreateSnapshotResponse(historyExtension.History(key, parameters));
        }

        public FhirResponse Mailbox(Bundle bundle, Binary body)
        {
            throw new NotImplementedException();
        }

        public FhirResponse Conformance(string sparkVersion)
        {
            IConformanceService conformanceService = this.GetFeature<IConformanceService>();

            return Respond.WithResource(conformanceService.GetSparkConformance(sparkVersion));
        }

        public FhirResponse GetPage(string snapshotkey, int index)
        {
            IPagingService pagingExtension = this.FindExtension<IPagingService>();
            if (pagingExtension == null)
                throw new NotSupportedException("Operation not supported");

            return responseFactory.GetFhirResponse(pagingExtension.StartPagination(snapshotkey).GetPage(index));
        }

        public FhirResponse HandleInteraction(Entry interaction)
        {
            switch (interaction.Method)
            {
                case Bundle.HTTPVerb.PUT:
                    return this.Put(interaction);
                case Bundle.HTTPVerb.POST:
                    return this.Create(interaction);
                case Bundle.HTTPVerb.DELETE:
                    return this.Delete(interaction);
                case Bundle.HTTPVerb.GET:
                    return this.VersionRead((Key)interaction.Key);
                default:
                    return Respond.Success;
            }
        }

        private static void ValidateKey(IKey key, bool withVersion = false)
        {
            Validate.HasTypeName(key);
            Validate.HasResourceId(key);
            if (withVersion)
            {
                Validate.HasVersion(key);
            }
            else
            {
                Validate.HasNoVersion(key);
            }
            Validate.Key(key);
        }

        private T GetFeature<T>() where T: IFhirServiceExtension
        {
            //TODO: return 501 - 	Requested HTTP operation not supported?

            T feature = this.FindExtension<T>();
            if (feature == null)
                throw new NotSupportedException("Operation not supported");

            return feature;
        }

        internal Entry Store(Entry entry)
        {
            Entry result = GetFeature<IResourceStorageService>()
             .Add(entry);
            serviceListener.Inform(entry);
            return result;
        }
    }
}