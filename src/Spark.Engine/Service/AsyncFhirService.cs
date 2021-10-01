using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Service;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Spark.Engine.Extensions;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Service
{
    public class AsyncFhirService : FhirServiceBase, IAsyncFhirService, IInteractionHandler
    {
        private readonly IFhirResponseFactory _responseFactory;
        private readonly ICompositeServiceListener _serviceListener;

        public AsyncFhirService(
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
        public AsyncFhirService(
            IFhirServiceExtension[] extensions,
            IFhirResponseFactory responseFactory,
            ITransfer transfer,
            ICompositeServiceListener serviceListener = null)
        {
            _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
            _serviceListener = serviceListener ?? throw new ArgumentNullException(nameof(serviceListener));

            foreach (var serviceExtension in extensions)
            {
                AddExtension(serviceExtension);
            }
        }

        public async Task<FhirResponse> AddMetaAsync(IKey key, Parameters parameters)
        {
            var storageService = GetFeature<IResourceStorageService>();
            var entry = await storageService.GetAsync(key).ConfigureAwait(false);
            if (entry != null && !entry.IsDeleted())
            {
                entry.Resource.AffixTags(parameters);
                await storageService.AddAsync(entry).ConfigureAwait(false);
            }
            return _responseFactory.GetMetadataResponse(entry, key);
        }

        public Task<FhirResponse> ConditionalCreateAsync(IKey key, Resource resource, IEnumerable<Tuple<string, string>> parameters)
        {
            return ConditionalCreateAsync(key, resource, SearchParams.FromUriParamList(parameters));
        }

        public async Task<FhirResponse> ConditionalCreateAsync(IKey key, Resource resource, SearchParams parameters)
        {
            var searchStore = GetFeature<ISearchService>();
            var transactionService = GetFeature<ITransactionService>();
            var operation = await ResourceManipulationOperationFactory.CreatePostAsync(resource, key, searchStore, parameters).ConfigureAwait(false);
            return await transactionService.HandleTransactionAsync(operation, this).ConfigureAwait(false);
        }

        public async Task<FhirResponse> ConditionalDeleteAsync(IKey key, IEnumerable<Tuple<string, string>> parameters)
        {
            var searchStore = GetFeature<ISearchService>();
            var transactionService = GetFeature<ITransactionService>();
            var operation = await ResourceManipulationOperationFactory.CreateDeleteAsync(key, searchStore, SearchParams.FromUriParamList(parameters)).ConfigureAwait(false);
            return await transactionService.HandleTransactionAsync(operation, this)
                .ConfigureAwait(false) ?? Respond.WithCode(HttpStatusCode.NotFound);
        }

        public async Task<FhirResponse> ConditionalUpdateAsync(IKey key, Resource resource, SearchParams parameters)
        {
            var searchStore = GetFeature<ISearchService>();
            var transactionService = GetFeature<ITransactionService>();

            // FIXME: if update receives a key with no version how do we handle concurrency?

            var operation = await ResourceManipulationOperationFactory.CreatePutAsync(resource, key, searchStore, parameters).ConfigureAwait(false);
            return await transactionService.HandleTransactionAsync(operation, this).ConfigureAwait(false);
        }

        public Task<FhirResponse> CapabilityStatementAsync(string sparkVersion)
        {
            var capabilityStatementService = GetFeature<ICapabilityStatementService>();
            var response = Respond.WithResource(capabilityStatementService.GetSparkCapabilityStatement(sparkVersion));
            return Task.FromResult(response);
        }

        public async Task<FhirResponse> CreateAsync(IKey key, Resource resource)
        {
            Validate.Key(key);
            Validate.HasTypeName(key);
            Validate.ResourceType(key, resource);

            key = key.CleanupForCreate();
            var result = await StoreAsync(Entry.POST(key, resource)).ConfigureAwait(false);
            return Respond.WithResource(HttpStatusCode.Created, result);
        }

        public async Task<FhirResponse> DeleteAsync(IKey key)
        {
            Validate.Key(key);
            Validate.HasNoVersion(key);

            var resourceStorage = GetFeature<IResourceStorageService>();

            var current = await resourceStorage.GetAsync(key).ConfigureAwait(false);
            if (current != null && current.IsPresent)
            {
                return await DeleteAsync(Entry.DELETE(key, DateTimeOffset.UtcNow)).ConfigureAwait(false);
            }
            return Respond.WithCode(HttpStatusCode.NoContent);
        }

        public async Task<FhirResponse> DeleteAsync(Entry entry)
        {
            Validate.Key(entry.Key);
            await StoreAsync(entry).ConfigureAwait(false);
            return Respond.WithCode(HttpStatusCode.NoContent);
        }

        public async Task<FhirResponse> GetPageAsync(string snapshotKey, int index)
        {
            var pagingExtension = GetFeature<IPagingService>();
            var snapshot = await pagingExtension.StartPaginationAsync(snapshotKey).ConfigureAwait(false);
            return _responseFactory.GetFhirResponse(await snapshot.GetPageAsync(index).ConfigureAwait(false));
        }

        public async Task<FhirResponse> HistoryAsync(HistoryParameters parameters)
        {
            var historyExtension = GetFeature<IHistoryService>();
            var snapshot = await historyExtension.HistoryAsync(parameters).ConfigureAwait(false);
            return await CreateSnapshotResponseAsync(snapshot).ConfigureAwait(false);
        }

        public async Task<FhirResponse> HistoryAsync(string type, HistoryParameters parameters)
        {
            var historyExtension = GetFeature<IHistoryService>();
            var snapshot = await historyExtension.HistoryAsync(type, parameters).ConfigureAwait(false);
            return await CreateSnapshotResponseAsync(snapshot).ConfigureAwait(false);
        }

        public async Task<FhirResponse> HistoryAsync(IKey key, HistoryParameters parameters)
        {
            var storageService = GetFeature<IResourceStorageService>();
            if (await storageService.GetAsync(key).ConfigureAwait(false) == null)
            {
                return Respond.NotFound(key);
            }
            var historyExtension = GetFeature<IHistoryService>();
            var snapshot = await historyExtension.HistoryAsync(key, parameters).ConfigureAwait(false);
            return await CreateSnapshotResponseAsync(snapshot).ConfigureAwait(false);
        }

        public Task<FhirResponse> PutAsync(IKey key, Resource resource)
        {
            Validate.HasResourceId(resource);
            Validate.IsResourceIdEqual(key, resource);
            return PutAsync(Entry.PUT(key, resource));
        }

        public async Task<FhirResponse> PutAsync(Entry entry)
        {
            Validate.Key(entry.Key);
            Validate.ResourceType(entry.Key, entry.Resource);
            Validate.HasTypeName(entry.Key);
            Validate.HasResourceId(entry.Key);

            var storageService = GetFeature<IResourceStorageService>();
            var current = await storageService.GetAsync(entry.Key.WithoutVersion()).ConfigureAwait(false);
            var result = await StoreAsync(entry).ConfigureAwait(false);
            return Respond.WithResource(current != null ? HttpStatusCode.OK : HttpStatusCode.Created, result);
        }

        public async Task<FhirResponse> ReadAsync(IKey key, ConditionalHeaderParameters parameters = null)
        {
            Validate.ValidateKey(key);
            var entry = await GetFeature<IResourceStorageService>().GetAsync(key).ConfigureAwait(false);
            return _responseFactory.GetFhirResponse(entry, key, parameters);
        }

        public async Task<FhirResponse> ReadMetaAsync(IKey key)
        {
            Validate.ValidateKey(key);
            var entry = await GetFeature<IResourceStorageService>().GetAsync(key).ConfigureAwait(false);
            return _responseFactory.GetMetadataResponse(entry, key);
        }

        public async Task<FhirResponse> SearchAsync(string type, SearchParams searchCommand, int pageIndex = 0)
        {
            var searchService = GetFeature<ISearchService>();
            var snapshot = await searchService.GetSnapshotAsync(type, searchCommand).ConfigureAwait(false);
            return await CreateSnapshotResponseAsync(snapshot, pageIndex).ConfigureAwait(false);
        }

        public async Task<FhirResponse> TransactionAsync(IList<Entry> interactions)
        {
            var transactionExtension = GetFeature<ITransactionService>();
            var responses = await transactionExtension.HandleTransactionAsync(interactions, this).ConfigureAwait(false);
            return _responseFactory.GetFhirResponse(responses, Bundle.BundleType.TransactionResponse);
        }

        public async Task<FhirResponse> TransactionAsync(Bundle bundle)
        {
            var transactionExtension = GetFeature<ITransactionService>();
            var responses = await transactionExtension.HandleTransactionAsync(bundle, this).ConfigureAwait(false);
            return _responseFactory.GetFhirResponse(responses, Bundle.BundleType.TransactionResponse);
        }

        public async Task<FhirResponse> UpdateAsync(IKey key, Resource resource)
        {
            return key.HasVersionId()
                ? await VersionSpecificUpdateAsync(key, resource).ConfigureAwait(false)
                : await PutAsync(key, resource).ConfigureAwait(false);
        }

        public async Task<FhirResponse> PatchAsync(IKey key, Parameters parameters)
        {
            if (parameters == null)
            {
                return new FhirResponse(HttpStatusCode.BadRequest);
            }
            var resourceStorage = GetFeature<IResourceStorageService>();
            var current = await resourceStorage.GetAsync(key.WithoutVersion()).ConfigureAwait(false);
            if (current != null && current.IsPresent)
            {
                var patchService = GetFeature<IPatchService>();
                try
                {
                    var resource = patchService.Apply(current.Resource, parameters);
                    return await PatchAsync(Entry.PATCH(current.Key.WithoutVersion(), resource)).ConfigureAwait(false);
                }
                catch
                {
                    return new FhirResponse(HttpStatusCode.BadRequest);
                }
            }

            return Respond.WithCode(HttpStatusCode.NotFound);
        }

        public async Task<FhirResponse> PatchAsync(Entry entry)
        {
            Validate.Key(entry.Key);
            Validate.ResourceType(entry.Key, entry.Resource);
            Validate.HasTypeName(entry.Key);
            Validate.HasResourceId(entry.Key);

            var result = await StoreAsync(entry).ConfigureAwait(false);

            return Respond.WithResource(HttpStatusCode.OK, result);
        }

        public Task<FhirResponse> ValidateOperationAsync(IKey key, Resource resource)
        {
            throw new NotImplementedException();
        }

        public async Task<FhirResponse> VersionReadAsync(IKey key)
        {
            Validate.ValidateKey(key, true);
            var entry = await GetFeature<IResourceStorageService>().GetAsync(key).ConfigureAwait(false);
            return _responseFactory.GetFhirResponse(entry, key);
        }

        public async Task<FhirResponse> VersionSpecificUpdateAsync(IKey versionedKey, Resource resource)
        {
            Validate.HasTypeName(versionedKey);
            Validate.HasVersion(versionedKey);
            var key = versionedKey.WithoutVersion();
            var current = await GetFeature<IResourceStorageService>().GetAsync(key).ConfigureAwait(false);
            Validate.IsSameVersion(current.Key, versionedKey);
            return await PutAsync(key, resource).ConfigureAwait(false);
        }

        public async Task<FhirResponse> EverythingAsync(IKey key)
        {
            var searchService = GetFeature<ISearchService>();
            var snapshot = await searchService.GetSnapshotForEverythingAsync(key).ConfigureAwait(false);
            return await CreateSnapshotResponseAsync(snapshot).ConfigureAwait(false);
        }

        public async Task<FhirResponse> DocumentAsync(IKey key)
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
            return await SearchAsync(key.TypeName, searchCommand).ConfigureAwait(false);
        }

        private async Task<FhirResponse> CreateAsync(Entry entry)
        {
            Validate.Key(entry.Key);
            Validate.HasTypeName(entry.Key);
            Validate.ResourceType(entry.Key, entry.Resource);

            if (entry.State != EntryState.Internal)
            {
                Validate.HasNoResourceId(entry.Key);
                Validate.HasNoVersion(entry.Key);
            }

            var result = await StoreAsync(entry).ConfigureAwait(false);
            return Respond.WithResource(HttpStatusCode.Created, result);
        }

        // TODO: Remove this when we have split interfaces into synchronous and asynchronous conunterparts
        public FhirResponse HandleInteraction(Entry interaction)
        {
            return Task.Run(() => HandleInteractionAsync(interaction)).GetAwaiter().GetResult();
        }

        public async Task<FhirResponse> HandleInteractionAsync(Entry interaction)
        {
            switch (interaction.Method)
            {
                case Bundle.HTTPVerb.PUT:
                    return await PutAsync(interaction).ConfigureAwait(false);
                case Bundle.HTTPVerb.POST:
                    return await CreateAsync(interaction).ConfigureAwait(false);
                case Bundle.HTTPVerb.DELETE:
                    var resourceStorage = GetFeature<IResourceStorageService>();
                    var current = await resourceStorage.GetAsync(interaction.Key.WithoutVersion()).ConfigureAwait(false);
                    if (current != null && current.IsPresent)
                    {
                        return await DeleteAsync(interaction).ConfigureAwait(false);
                    }
                    // FIXME: there's no way to distinguish between "successfully deleted"
                    // and "resource not deleted because it doesn't exist" responses, all return NoContent.
                    // Same with Delete method above.
                    return Respond.WithCode(HttpStatusCode.NoContent);
                case Bundle.HTTPVerb.GET:
                    if (interaction.Key.HasVersionId())
                    {
                        return await VersionReadAsync((Key)interaction.Key).ConfigureAwait(false);
                    }
                    else
                    {
                        return await ReadAsync(interaction.Key).ConfigureAwait(false);
                    }
                case Bundle.HTTPVerb.PATCH:
                    return await PatchAsync(interaction.Key, interaction.Resource as Parameters).ConfigureAwait(false);
                default:
                    return Respond.Success;
            }
        }

        private async Task<FhirResponse> CreateSnapshotResponseAsync(Snapshot snapshot, int pageIndex = 0)
        {
            var pagingExtension = FindExtension<IPagingService>();
            if (pagingExtension == null)
            {
                var bundle = new Bundle
                {
                    Type = snapshot.Type,
                    Total = snapshot.Count
                };
                var resourceStorage = FindExtension<IResourceStorageService>();
                bundle.Append(await resourceStorage.GetAsync(snapshot.Keys).ConfigureAwait(false));
                return _responseFactory.GetFhirResponse(bundle);
            }
            else
            {
                var pagination = await pagingExtension.StartPaginationAsync(snapshot).ConfigureAwait(false);
                var bundle = await pagination.GetPageAsync(pageIndex).ConfigureAwait(false);
                return _responseFactory.GetFhirResponse(bundle);
            }
        }

        internal async Task<Entry> StoreAsync(Entry entry)
        {
            var result = await GetFeature<IResourceStorageService>()
                .AddAsync(entry).ConfigureAwait(false);
            await _serviceListener.InformAsync(entry).ConfigureAwait(false);
            return result;
        }
    }
}
