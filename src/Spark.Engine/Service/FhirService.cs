using System;
using System.Collections.Generic;
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Service.Abstractions;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Service;
using System.Linq;

namespace Spark.Engine.Service
{
    public class FhirService : FhirServiceBase, IInteractionHandler
    {
        public FhirService(
            IFhirServiceExtension[] extensions,
            IFhirResponseFactory responseFactory,
            ICompositeServiceListener serviceListener = null)
        : base(extensions, responseFactory, serviceListener)
        {
        }

        [Obsolete("This constructor is obsolete. Please use constructor with signature ctor(IFhirServiceExtension[], IFhirResponseFactory, ICompositeServiceListener")]
        public FhirService(IFhirServiceExtension[] extensions,
            IFhirResponseFactory responseFactory,
            // ReSharper disable once UnusedParameter.Local
            ITransfer transfer,
            ICompositeServiceListener serviceListener = null)
        : base(extensions, responseFactory, serviceListener)
        {
        }

        public override FhirResponse AddMeta(IKey key, Parameters parameters)
        {
            var storageService = GetFeature<IResourceStorageService>();
            var entry = storageService.Get(key);
            if (entry != null && entry.IsDeleted() == false)
            {
                var metaResource = parameters.ExtractMetaResources().SingleOrDefault();
                entry.Resource.Meta.Merge(metaResource);
                storageService.Add(entry);
            }

            return _responseFactory.GetMetadataResponse(entry, key);
        }

        public override FhirResponse ConditionalCreate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> parameters)
        {
            return ConditionalCreate(key, resource, SearchParams.FromUriParamList(parameters));
        }

        public override FhirResponse ConditionalCreate(IKey key, Resource resource, SearchParams parameters)
        {
            var searchStore = GetFeature<ISearchService>();
            var transactionService = GetFeature<ITransactionService>();
            var operation = ResourceManipulationOperationFactory.CreatePost(resource, key, searchStore, parameters);
            return transactionService.HandleTransaction(operation, this);
        }

        public override FhirResponse ConditionalDelete(IKey key, IEnumerable<Tuple<string, string>> parameters)
        {
            var searchStore = GetFeature<ISearchService>();
            var transactionService = GetFeature<ITransactionService>();
            var operation = ResourceManipulationOperationFactory.CreateDelete(key, searchStore, SearchParams.FromUriParamList(parameters));
            return transactionService.HandleTransaction(operation, this) 
                   ?? Respond.WithCode(HttpStatusCode.NotFound);
        }

        public override FhirResponse ConditionalUpdate(IKey key, Resource resource, SearchParams parameters)
        {
            //if update receives a key with no version how do we handle concurrency?
            ISearchService searchStore = GetFeature<ISearchService>();
            ITransactionService transactionService = GetFeature<ITransactionService>();
            var operation = ResourceManipulationOperationFactory.CreatePut(resource, key, searchStore, parameters); 
            return transactionService.HandleTransaction(operation, this);
        }
        
        public override FhirResponse CapabilityStatement(string sparkVersion)
        {
            var capabilityStatementService = GetFeature<ICapabilityStatementService>();
            return Respond.WithResource(capabilityStatementService.GetSparkCapabilityStatement(sparkVersion));
        }
        
        public override FhirResponse Create(IKey key, Resource resource)
        {
            Validate.Key(key);
            Validate.HasTypeName(key);
            Validate.ResourceType(key, resource);

            key = key.CleanupForCreate();
            var result = Store(Entry.POST(key, resource));
            return Respond.WithResource(HttpStatusCode.Created, result);
        }
        
        public override FhirResponse Delete(IKey key)
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

        public override FhirResponse Delete(Entry entry)
        {
            Validate.Key(entry.Key);
            Store(entry);
            return Respond.WithCode(HttpStatusCode.NoContent);
        }
        
        public override FhirResponse GetPage(string snapshotKey, int index)
        {
            var pagingExtension = GetFeature<IPagingService>();
            return _responseFactory.GetFhirResponse(pagingExtension.StartPagination(snapshotKey).GetPage(index));
        }

        public override FhirResponse History(HistoryParameters parameters)
        {
            var historyExtension = this.GetFeature<IHistoryService>();
            return CreateSnapshotResponse(historyExtension.History(parameters));
        }

        public override FhirResponse History(string type, HistoryParameters parameters)
        {
            var historyExtension = this.GetFeature<IHistoryService>();
            return CreateSnapshotResponse(historyExtension.History(type, parameters));
        }

        public override FhirResponse History(IKey key, HistoryParameters parameters)
        {
            var storageService = GetFeature<IResourceStorageService>();
            if (storageService.Get(key) == null)
            {
                return Respond.NotFound(key);
            }
            var historyExtension = this.GetFeature<IHistoryService>();
            return CreateSnapshotResponse(historyExtension.History(key, parameters));
        }

        public override FhirResponse Put(IKey key, Resource resource)
        {
            Validate.HasResourceId(resource);
            Validate.IsResourceIdEqual(key, resource);
            return Put(Entry.PUT(key, resource));
        }
        
        public override FhirResponse Put(Entry entry)
        {
            Validate.Key(entry.Key);
            Validate.ResourceType(entry.Key, entry.Resource);
            Validate.HasTypeName(entry.Key);
            Validate.HasResourceId(entry.Key);

            var storageService = GetFeature<IResourceStorageService>();
            var current = storageService.Get(entry.Key.WithoutVersion());
            var result = Store(entry);
            return Respond.WithResource(current != null ? HttpStatusCode.OK : HttpStatusCode.Created, result);
        }

        public override FhirResponse Read(IKey key, ConditionalHeaderParameters parameters = null)
        {
            Validate.ValidateKey(key);
            var entry = GetFeature<IResourceStorageService>().Get(key);
            return _responseFactory.GetFhirResponse(entry, key, parameters);
        }

        public override FhirResponse ReadMeta(IKey key)
        {
            Validate.ValidateKey(key);
            var entry = GetFeature<IResourceStorageService>().Get(key);
            return _responseFactory.GetMetadataResponse(entry, key);
        }

        public override FhirResponse Search(string type, SearchParams searchCommand, int pageIndex = 0)
        {
            var searchService = this.GetFeature<ISearchService>();
            var snapshot = searchService.GetSnapshot(type, searchCommand);
            return CreateSnapshotResponse(snapshot, pageIndex);
        }

        public override FhirResponse Transaction(IList<Entry> interactions)
        {
            var transactionExtension = this.GetFeature<ITransactionService>();
            var responses = transactionExtension.HandleTransaction(interactions, this); 
            return _responseFactory.GetFhirResponse(responses, Bundle.BundleType.TransactionResponse);
        }

        public override FhirResponse Transaction(Bundle bundle)
        {
            var transactionExtension = this.GetFeature<ITransactionService>();
            var responses = transactionExtension.HandleTransaction(bundle, this);
            return _responseFactory.GetFhirResponse(responses, Bundle.BundleType.TransactionResponse);
        }

        public override FhirResponse Update(IKey key, Resource resource)
        {
            return key.HasVersionId() 
                ? this.VersionSpecificUpdate(key, resource)
                : this.Put(key, resource);
        }
        
        public override FhirResponse Patch(IKey key, Parameters parameters)
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
                    return Patch(Entry.PATCH(current.Key.WithoutVersion(), resource));
                }
                catch
                {
                    return new FhirResponse(HttpStatusCode.BadRequest);
                }
            }

            return Respond.WithCode(HttpStatusCode.NotFound);
        }

        public FhirResponse Patch(Entry entry)
        {
            Validate.Key(entry.Key);
            Validate.ResourceType(entry.Key, entry.Resource);
            Validate.HasTypeName(entry.Key);
            Validate.HasResourceId(entry.Key);

            var result = Store(entry);

            return Respond.WithResource(HttpStatusCode.OK, result);
        }

        public override FhirResponse ValidateOperation(IKey key, Resource resource)
        {
            throw new NotImplementedException();
        }

        public override FhirResponse VersionRead(IKey key)
        {
            Validate.ValidateKey(key, true);
            var entry = GetFeature<IResourceStorageService>().Get(key);
            return _responseFactory.GetFhirResponse(entry, key);
        }

        public override FhirResponse VersionSpecificUpdate(IKey versionedkey, Resource resource)
        {
            Validate.HasTypeName(versionedkey);
            Validate.HasVersion(versionedkey);
            Key key = versionedkey.WithoutVersion();
            Entry current = GetFeature<IResourceStorageService>().Get(key);
            Validate.IsSameVersion(current.Key, versionedkey);
            return this.Put(key, resource);
        }

        public override FhirResponse Everything(IKey key)
        {
            var searchService = GetFeature<ISearchService>();
            var snapshot = searchService.GetSnapshotForEverything(key);
            return CreateSnapshotResponse(snapshot);
        }

        public override FhirResponse Document(IKey key)
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
                case Bundle.HTTPVerb.PATCH:
                    return Patch(interaction.Key, interaction.Resource as Parameters);
                default:
                    return Respond.Success;
            }
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
    }
}
