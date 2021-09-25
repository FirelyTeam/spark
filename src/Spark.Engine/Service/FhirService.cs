using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Service;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Service
{
    public class FhirService : FhirServiceBase, IFhirService, IInteractionHandler
    {
        private readonly IFhirResponseFactory _responseFactory;
        private readonly ICompositeServiceListener _serviceListener;

        public FhirService(
            IFhirServiceExtension[] extensions,
            IFhirResponseFactory responseFactory,
            ICompositeServiceListener serviceListener = null)
        {
            _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
            _serviceListener = serviceListener ?? throw new ArgumentNullException(nameof(serviceListener));

            foreach (var serviceExtension in extensions)
            {
                AddExtension(serviceExtension);
            }
        }

        [Obsolete("This constructor is obsolete. Please use constructor with signature ctor(IFhirServiceExtension[], IFhirResponseFactory, ICompositeServiceListener")]
        public FhirService(IFhirServiceExtension[] extensions,
            IFhirResponseFactory responseFactory,
            ITransfer transfer,
            ICompositeServiceListener serviceListener = null)
        {
            this._responseFactory = responseFactory;
            this._serviceListener = serviceListener;

            foreach (IFhirServiceExtension serviceExtension in extensions)
            {
                this.AddExtension(serviceExtension);
            }
        }

        public FhirResponse AddMeta(IKey key, Parameters parameters)
        {
            var storageService = GetFeature<IResourceStorageService>();
            var entry = storageService.Get(key);
            if (entry != null && entry.IsDeleted() == false)
            {
                entry.Resource.AffixTags(parameters);
                storageService.Add(entry);
            }

            return _responseFactory.GetMetadataResponse(entry, key);
        }

        public FhirResponse ConditionalCreate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> parameters)
        {
            return ConditionalCreate(key, resource, SearchParams.FromUriParamList(parameters));
        }

        public FhirResponse ConditionalCreate(IKey key, Resource resource, SearchParams parameters)
        {
            return ConditionalCreate(key, resource, parameters, Prefer.ReturnRepresentation);
        }

        public FhirResponse ConditionalCreate(IKey key, Resource resource, SearchParams parameters, Prefer prefer = Prefer.ReturnRepresentation)
        {
            var searchStore = GetFeature<ISearchService>();
            var transactionService = GetFeature<ITransactionService>();
            var operation = ResourceManipulationOperationFactory.CreatePost(resource, key, searchStore, parameters, prefer);
            return transactionService.HandleTransaction(operation, this);
        }

        public FhirResponse ConditionalDelete(IKey key, IEnumerable<Tuple<string, string>> parameters)
        {
            var searchStore = GetFeature<ISearchService>();
            var transactionService = GetFeature<ITransactionService>();
            var operation = ResourceManipulationOperationFactory.CreateDelete(key, searchStore, SearchParams.FromUriParamList(parameters));
            return transactionService.HandleTransaction(operation, this) 
                   ?? Respond.WithCode(HttpStatusCode.NotFound);
        }

        public FhirResponse ConditionalUpdate(IKey key, Resource resource, SearchParams parameters)
        {
            return ConditionalUpdate(key, resource, parameters, Prefer.ReturnRepresentation);
        }

        public FhirResponse ConditionalUpdate(IKey key, Resource resource, SearchParams parameters, Prefer prefer = Prefer.ReturnRepresentation)
        {
            //if update receives a key with no version how do we handle concurrency?
            ISearchService searchStore = GetFeature<ISearchService>();
            ITransactionService transactionService = GetFeature<ITransactionService>();
            var operation = ResourceManipulationOperationFactory.CreatePut(resource, key, searchStore, parameters, prefer);
            return transactionService.HandleTransaction(operation, this);
        }
        
        public FhirResponse CapabilityStatement(string sparkVersion)
        {
            var capabilityStatementService = GetFeature<ICapabilityStatementService>();
            return Respond.WithResource(capabilityStatementService.GetSparkCapabilityStatement(sparkVersion));
        }
        
        public FhirResponse Create(IKey key, Resource resource)
        {
            return Create(key, resource, Prefer.ReturnRepresentation);
        }

        public FhirResponse Create(IKey key, Resource resource, Prefer prefer = Prefer.ReturnRepresentation)
        {
            Validate.Key(key);
            Validate.HasTypeName(key);
            Validate.ResourceType(key, resource);

            key = key.CleanupForCreate();
            var result = Store(Entry.POST(key, resource));
            return Respond.WithResource(HttpStatusCode.Created, result, prefer);
        }
        
        public FhirResponse Delete(IKey key)
        {
            Validate.Key(key);
            Validate.HasNoVersion(key);

            var resourceStorage = GetFeature<IResourceStorageService>();

            var current = resourceStorage.Get(key);
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
        
        public FhirResponse GetPage(string snapshotKey, int index)
        {
            var pagingExtension = GetFeature<IPagingService>();
            return _responseFactory.GetFhirResponse(pagingExtension.StartPagination(snapshotKey).GetPage(index));
        }

        public FhirResponse History(HistoryParameters parameters)
        {
            var historyExtension = this.GetFeature<IHistoryService>();
            return CreateSnapshotResponse(historyExtension.History(parameters));
        }

        public FhirResponse History(string type, HistoryParameters parameters)
        {
            var historyExtension = this.GetFeature<IHistoryService>();
            return CreateSnapshotResponse(historyExtension.History(type, parameters));
        }

        public FhirResponse History(IKey key, HistoryParameters parameters)
        {
            var storageService = GetFeature<IResourceStorageService>();
            if (storageService.Get(key) == null)
            {
                return Respond.NotFound(key);
            }
            var historyExtension = this.GetFeature<IHistoryService>();
            return CreateSnapshotResponse(historyExtension.History(key, parameters));
        }

        public FhirResponse Mailbox(Bundle bundle, Binary body)
        {
            throw new NotImplementedException();
        }

        public FhirResponse Put(IKey key, Resource resource)
        {
            return Put(key, resource, Prefer.ReturnRepresentation);
        }

        public FhirResponse Put(IKey key, Resource resource, Prefer prefer = Prefer.ReturnRepresentation)
        {
            Validate.HasResourceId(resource);
            Validate.IsResourceIdEqual(key, resource);
            return Put(Entry.PUT(key, resource, prefer));
        }
        
        public FhirResponse Put(Entry entry)
        {
            Validate.Key(entry.Key);
            Validate.ResourceType(entry.Key, entry.Resource);
            Validate.HasTypeName(entry.Key);
            Validate.HasResourceId(entry.Key);

            var storageService = GetFeature<IResourceStorageService>();
            var current = storageService.Get(entry.Key.WithoutVersion());
            var result = Store(entry);
            return Respond.WithResource(current != null ? HttpStatusCode.OK : HttpStatusCode.Created, result, entry.Prefer);
        }

        public FhirResponse Read(IKey key, ConditionalHeaderParameters parameters = null)
        {
            ValidateKey(key);
            var entry = GetFeature<IResourceStorageService>().Get(key);
            return _responseFactory.GetFhirResponse(entry, key, parameters);
        }

        public FhirResponse ReadMeta(IKey key)
        {
            ValidateKey(key);
            var entry = GetFeature<IResourceStorageService>().Get(key);
            return _responseFactory.GetMetadataResponse(entry, key);
        }

        public FhirResponse Search(string type, SearchParams searchCommand, int pageIndex = 0)
        {
            var searchService = this.GetFeature<ISearchService>();
            var snapshot = searchService.GetSnapshot(type, searchCommand);
            return CreateSnapshotResponse(snapshot, pageIndex);
        }

        public FhirResponse Transaction(IList<Entry> interactions)
        {
            var transactionExtension = this.GetFeature<ITransactionService>();
            var responses = transactionExtension.HandleTransaction(interactions, this); 
            return _responseFactory.GetFhirResponse(responses, Bundle.BundleType.TransactionResponse);
        }

        public FhirResponse Transaction(Bundle bundle)
        {
            var transactionExtension = this.GetFeature<ITransactionService>();
            var responses = transactionExtension.HandleTransaction(bundle, this);
            return _responseFactory.GetFhirResponse(responses, Bundle.BundleType.TransactionResponse);
        }

        public FhirResponse Update(IKey key, Resource resource)
        {
            return Update(key, resource, Prefer.ReturnRepresentation);
        }

        public FhirResponse Update(IKey key, Resource resource, Prefer prefer = Prefer.ReturnRepresentation)
        {
            return key.HasVersionId() 
                ? this.VersionSpecificUpdate(key, resource, prefer)
                : this.Put(key, resource, prefer);
        }
        
        public FhirResponse Patch(IKey key, Parameters parameters)
        {
            return Patch(key, parameters, Prefer.ReturnRepresentation);
        }

        public FhirResponse Patch(IKey key, Parameters parameters, Prefer prefer = Prefer.ReturnRepresentation)
        {
            if (parameters == null)
            {
                return new FhirResponse(HttpStatusCode.BadRequest);
            }
            var resourceStorage = GetFeature<IResourceStorageService>();
            var current = resourceStorage.Get(key.WithoutVersion());
            if (current != null && current.IsPresent)
            {
                var patchService = GetFeature<IPatchService>();
                try
                {
                    var resource = patchService.Apply(current.Resource, parameters);
                    return Put(Entry.PUT(current.Key.WithoutVersion(), resource, prefer));
                }
                catch
                {
                    return new FhirResponse(HttpStatusCode.BadRequest);
                }
            }

            return Respond.WithCode(HttpStatusCode.NotFound);
        }

        public FhirResponse ValidateOperation(IKey key, Resource resource)
        {
            throw new NotImplementedException();
        }

        public FhirResponse VersionRead(IKey key)
        {
            ValidateKey(key, true);
            var entry = GetFeature<IResourceStorageService>().Get(key);
            return _responseFactory.GetFhirResponse(entry, key);
        }

        public FhirResponse VersionSpecificUpdate(IKey versionedkey, Resource resource)
        {
            return VersionSpecificUpdate(versionedkey, resource, Prefer.ReturnRepresentation);
        }

        public FhirResponse VersionSpecificUpdate(IKey versionedkey, Resource resource, Prefer prefer = Prefer.ReturnRepresentation)
        {
            Validate.HasTypeName(versionedkey);
            Validate.HasVersion(versionedkey);
            Key key = versionedkey.WithoutVersion();
            Entry current = GetFeature<IResourceStorageService>().Get(key);
            Validate.IsSameVersion(current.Key, versionedkey);
            return Put(key, resource, prefer);
        }

        public FhirResponse Everything(IKey key)
        {
            var searchService = GetFeature<ISearchService>();
            var snapshot = searchService.GetSnapshotForEverything(key);
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
                searchCommand.Include.Add((inc, IncludeModifier.None));
            }
            return Search(key.TypeName, searchCommand);
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

            var result = Store(entry);
            return Respond.WithResource(HttpStatusCode.Created, result);
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
                    var resourceStorage = GetFeature<IResourceStorageService>();
                    var current = resourceStorage.Get(interaction.Key.WithoutVersion());
                    if (current != null && current.IsPresent)
                    {
                        return this.Delete(interaction);
                    }
                    // FIXME: there's no way to distinguish between "successfully deleted"
                    // and "resource not deleted because it doesn't exist" responses, all return NoContent.
                    // Same with Delete method above.
                    return Respond.WithCode(HttpStatusCode.NoContent);
                case Bundle.HTTPVerb.GET:
                    if (interaction.Key.HasVersionId())
                    {
                        return this.VersionRead((Key)interaction.Key);
                    }
                    else
                    {
                        return Read(interaction.Key);
                    }
                default:
                    return Respond.Success;
            }
        }

        // TODO: Remove this when we have split interfaces into synchronous and asynchronous conunterparts
        public Task<FhirResponse> HandleInteractionAsync(Entry interaction)
        {
            return Task.FromResult(HandleInteraction(interaction));
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
                return _responseFactory.GetFhirResponse(bundle);
            }
            else
            {
                Bundle bundle = pagingExtension.StartPagination(snapshot).GetPage(pageIndex);
                return _responseFactory.GetFhirResponse(bundle);
            }
        }

        internal Entry Store(Entry entry)
        {
            Entry result = GetFeature<IResourceStorageService>()
             .Add(entry);
            _serviceListener.Inform(entry);
            return result;
        }
    }
}