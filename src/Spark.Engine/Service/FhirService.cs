﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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

        public async Task<FhirResponse> Read(IKey key, ConditionalHeaderParameters parameters = null)
        {
            ValidateKey(key);

            Entry entry = await GetFeature<IResourceStorageService>().Get(key);

            return responseFactory.GetFhirResponse(entry, key, parameters);
        }

        public async Task<FhirResponse> ReadMeta(IKey key)
        {
            ValidateKey(key);

            Entry entry = await GetFeature<IResourceStorageService>().Get(key);

            return responseFactory.GetMetadataResponse(entry, key);
        }

        public async Task<FhirResponse> AddMeta(IKey key, Parameters parameters)
        {
            var storageService = GetFeature<IResourceStorageService>();
            Entry entry = await storageService.Get(key);

            if (entry != null && entry.IsDeleted() == false)
            {
                entry.Resource.AffixTags(parameters);
                await storageService.Add(entry);
            }

            return responseFactory.GetMetadataResponse(entry, key);
        }

        public async Task<FhirResponse> VersionRead(IKey key)
        {
            ValidateKey(key, true);
            Entry entry = await GetFeature<IResourceStorageService>().Get(key);

            return responseFactory.GetFhirResponse(entry, key);
        }

        public async Task<FhirResponse> Create(IKey key, Resource resource)
        {
            Validate.Key(key);
            Validate.HasTypeName(key);
            Validate.ResourceType(key, resource);

            Validate.HasNoResourceId(key);
            Validate.HasNoVersion(key);

            Entry result = await Store(Entry.POST(key, resource));

            return Respond.WithResource(HttpStatusCode.Created, result);
        }

        public async Task<FhirResponse> Create(Entry entry)
        {
            Validate.Key(entry.Key);
            Validate.HasTypeName(entry.Key);
            Validate.ResourceType(entry.Key, entry.Resource);

            if (entry.State != EntryState.Internal)
            {
                Validate.HasNoResourceId(entry.Key);
                Validate.HasNoVersion(entry.Key);
            }

            Entry result = await Store(entry);

            return Respond.WithResource(HttpStatusCode.Created, result);
        }

        public async Task<FhirResponse> Put(Entry entry)
        {
            Validate.Key(entry.Key);
            Validate.ResourceType(entry.Key, entry.Resource);
            Validate.HasTypeName(entry.Key);
            Validate.HasResourceId(entry.Key);

            var storageService = GetFeature<IResourceStorageService>();
            Entry current = await storageService.Get(entry.Key.WithoutVersion());

            Entry result = await Store(entry);

            return Respond.WithResource(current != null ? HttpStatusCode.OK : HttpStatusCode.Created, result);

        }

        public async Task<FhirResponse> Put(IKey key, Resource resource)
        {
            Validate.HasResourceId(resource);
            Validate.IsResourceIdEqual(key, resource);
            return await Put(Entry.PUT(key, resource));
        }

        public Task<FhirResponse> ConditionalCreate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> parameters)
        {
            return ConditionalCreate(key, resource, SearchParams.FromUriParamList(parameters));
        }

        public async Task<FhirResponse> ConditionalCreate(IKey key, Resource resource, SearchParams parameters)
        {
            ISearchService searchStore = this.FindExtension<ISearchService>();
            ITransactionService transactionService = this.FindExtension<ITransactionService>();

            if (searchStore == null || transactionService == null)
                throw new NotSupportedException("Operation not supported");

            var post = await ResourceManipulationOperationFactory.CreatePost(resource, key, searchStore, parameters);
            return await transactionService.HandleTransaction(post, this);
        }

        public async Task<FhirResponse> Everything(IKey key)
        {
            ISearchService searchService = this.GetFeature<ISearchService>();

            Snapshot snapshot = await searchService.GetSnapshotForEverything(key);

            return await CreateSnapshotResponse(snapshot);
        }

        public Task<FhirResponse> Document(IKey key)
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

        public async Task<FhirResponse> VersionSpecificUpdate(IKey versionedkey, Resource resource)
        {
            Validate.HasTypeName(versionedkey);
            Validate.HasVersion(versionedkey);
            Key key = versionedkey.WithoutVersion();
            Entry current = await GetFeature<IResourceStorageService>().Get(key);
            Validate.IsSameVersion(current.Key, versionedkey);

            return await Put(key, resource);
        }

        public Task<FhirResponse> Update(IKey key, Resource resource)
        {
            if (key.HasVersionId())
                return VersionSpecificUpdate(key, resource);
            else
                return Put(key, resource);
        }

        public Task<FhirResponse> ConditionalUpdate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> parameters)
        {
            return ConditionalUpdate(key, resource, SearchParams.FromUriParamList(parameters));
        }

        public async Task<FhirResponse> ConditionalUpdate(IKey key, Resource resource, SearchParams _params)
        {
            //if update receives a key with no version how do we handle concurrency?
            ISearchService searchStore = this.FindExtension<ISearchService>();
            ITransactionService transactionService = this.FindExtension<ITransactionService>();
            if (searchStore == null || transactionService == null)
                throw new NotSupportedException("Operation not supported");

            var put = await ResourceManipulationOperationFactory.CreatePut(resource, key, searchStore, _params);
            return await transactionService.HandleTransaction(put, this);
        }

        public async Task<FhirResponse> Delete(IKey key)
        {
            Validate.Key(key);
            Validate.HasNoVersion(key);

            var resourceStorage = GetFeature<IResourceStorageService>();

            Entry current = await resourceStorage.Get(key);
            if (current != null && current.IsPresent)
            {
                return await Delete(Entry.DELETE(key, DateTimeOffset.UtcNow));
            }
            return Respond.WithCode(HttpStatusCode.NoContent);
        }

        public async Task<FhirResponse> Delete(Entry entry)
        {
            Validate.Key(entry.Key);
            await Store(entry);
            return Respond.WithCode(HttpStatusCode.NoContent);
        }

        public Task<FhirResponse> ConditionalDelete(IKey key, IEnumerable<Tuple<string, string>> parameters)
        {
            return ConditionalDelete(key, SearchParams.FromUriParamList(parameters));
        }

        public async Task<FhirResponse> ConditionalDelete(IKey key, SearchParams _params)
        {
            ISearchService searchStore = this.FindExtension<ISearchService>();
            ITransactionService transactionService = this.FindExtension<ITransactionService>();
            if (searchStore == null || transactionService == null)
                throw new NotSupportedException("Operation not supported");

            var delete = await ResourceManipulationOperationFactory.CreateDelete(key, searchStore, _params);
            var trans = await transactionService.HandleTransaction(delete, this);
            return trans ?? Respond.WithCode(HttpStatusCode.NotFound);
        }

        public async Task<FhirResponse> ValidateOperation(IKey key, Resource resource)
        {
            await System.Threading.Tasks.Task.CompletedTask;

            if (resource == null) throw Error.BadRequest("Validate needs a Resource in the body payload");
            Validate.ResourceType(key, resource);

            // DSTU2: validation
            var outcome = Validate.AgainstSchema(resource);

            if (outcome == null)
                return Respond.WithCode(HttpStatusCode.OK);
            else
                return Respond.WithResource(422, outcome);
        }

        public async Task<FhirResponse> Search(string type, SearchParams searchCommand, int pageIndex = 0)
        {
            ISearchService searchService = this.GetFeature<ISearchService>();
            Snapshot snapshot = await searchService.GetSnapshot(type, searchCommand);

            return await CreateSnapshotResponse(snapshot, pageIndex);
        }

        private async Task<FhirResponse> CreateSnapshotResponse(Snapshot snapshot, int pageIndex = 0)
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
                bundle.Append(await resourceStorage.Get(snapshot.Keys));
                return responseFactory.GetFhirResponse(bundle);
            }
            else
            {
                Bundle bundle = await pagingExtension.StartPagination(snapshot).GetPage(pageIndex);
                return responseFactory.GetFhirResponse(bundle);
            }
        }

        public async Task<FhirResponse> Transaction(IList<Entry> interactions)
        {
            await System.Threading.Tasks.Task.CompletedTask;
            ITransactionService transactionExtension = this.GetFeature<ITransactionService>();
            return responseFactory.GetFhirResponse(
                await transactionExtension.HandleTransaction(interactions, this),
                Bundle.BundleType.TransactionResponse);
        }

        public async Task<FhirResponse> Transaction(Bundle bundle)
        {
            await System.Threading.Tasks.Task.CompletedTask;
            ITransactionService transactionExtension = this.GetFeature<ITransactionService>();
            return responseFactory.GetFhirResponse(
                await transactionExtension.HandleTransaction(bundle, this),
                Bundle.BundleType.TransactionResponse);
        }

        public async Task<FhirResponse> History(HistoryParameters parameters)
        {
            IHistoryService historyExtension = this.GetFeature<IHistoryService>();

            var h = await historyExtension.History(parameters);
            return await CreateSnapshotResponse(h);
        }

        public async Task<FhirResponse> History(string type, HistoryParameters parameters)
        {
            IHistoryService historyExtension = this.GetFeature<IHistoryService>();

            var h = await historyExtension.History(type, parameters);
            return await CreateSnapshotResponse(h);
        }

        public async Task<FhirResponse> History(IKey key, HistoryParameters parameters)
        {
            IResourceStorageService storageService = GetFeature<IResourceStorageService>();
            if (storageService.Get(key) == null)
            {
                return Respond.NotFound(key);
            }
            IHistoryService historyExtension = this.GetFeature<IHistoryService>();

            var h = await historyExtension.History(key, parameters);
            return await CreateSnapshotResponse(h);
        }

        public Task<FhirResponse> Mailbox(Bundle bundle, Binary body)
        {
            throw new NotImplementedException();
        }

        public async Task<FhirResponse> CapabilityStatement(string sparkVersion)
        {
            await System.Threading.Tasks.Task.CompletedTask;

            ICapabilityStatementService capabilityStatementService = this.GetFeature<ICapabilityStatementService>();

            return Respond.WithResource(capabilityStatementService.GetSparkCapabilityStatement(sparkVersion));
        }

        public async Task<FhirResponse> GetPage(string snapshotkey, int index)
        {
            IPagingService pagingExtension = this.FindExtension<IPagingService>();
            if (pagingExtension == null)
                throw new NotSupportedException("Operation not supported");

            var snapshot = await pagingExtension.StartPagination(snapshotkey);
            var page = await snapshot.GetPage(index);
            return responseFactory.GetFhirResponse(page);
        }

        public async Task<FhirResponse> HandleInteraction(Entry interaction)
        {
            switch (interaction.Method)
            {
                case Bundle.HTTPVerb.PUT:
                    return await this.Put(interaction);
                case Bundle.HTTPVerb.POST:
                    return await this.Create(interaction);
                case Bundle.HTTPVerb.DELETE:
                    return await this.Delete(interaction);
                case Bundle.HTTPVerb.GET:
                    return await this.VersionRead((Key)interaction.Key);
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

        private T GetFeature<T>() where T : IFhirServiceExtension
        {
            //TODO: return 501 - 	Requested HTTP operation not supported?

            T feature = this.FindExtension<T>();
            if (feature == null)
                throw new NotSupportedException("Operation not supported");

            return feature;
        }

        internal async Task<Entry> Store(Entry entry)
        {
            var result = await GetFeature<IResourceStorageService>().Add(entry);
            await serviceListener.Inform(entry);
            return result;
        }
    }
}
