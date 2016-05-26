using System;
using System.Collections.Generic;
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Store.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service
{
    public class FhirService : IFhirService
    {
        private readonly IFhirStore fhirStore;
        private readonly IFhirResponseFactory responseFactory;
        private readonly ITransfer transfer;
        private readonly IFhirModel fhirModel;

        public FhirService(IFhirStore fhirStore, IFhirResponseFactory responseFactory, 
            ITransfer transfer, IFhirModel fhirModel)
        {
            this.fhirStore = fhirStore;
            this.responseFactory = responseFactory;
            this.transfer = transfer;
            this.fhirModel = fhirModel;
        }

        public FhirResponse Read(Key key, ConditionalHeaderParameters parameters = null)
        {
            ValidateKey(key);
            Entry entry = fhirStore.Get(key);
            return responseFactory.GetFhirResponse(entry, key, parameters);
        }

        public FhirResponse ReadMeta(Key key)
        {
            ValidateKey(key);
            Entry entry = fhirStore.Get(key);
            return responseFactory.GetMetadataResponse(entry, key);
        }

        public FhirResponse AddMeta(Key key, Parameters parameters)
        {
            Entry entry = fhirStore.Get(key);

            if (entry != null && entry.IsDeleted() == false)
            {
                entry.Resource.AffixTags(parameters);
                fhirStore.Add(entry);
            }

            return responseFactory.GetMetadataResponse(entry, key);
        }

        public FhirResponse VersionRead(Key key)
        {
            ValidateKey(key, true);
            Entry entry = fhirStore.Get(key);

            return responseFactory.GetFhirResponse(entry, key);
        }

        public FhirResponse Create(IKey key, Resource resource)
        {
            Validate.Key(key);
            Validate.HasTypeName(key);
            Validate.ResourceType(key, resource);
            Validate.HasNoResourceId(key);
            Validate.HasNoVersion(key);

            Entry entry = Entry.POST(key, resource);
            transfer.Internalize(entry);
            fhirStore.Add(entry);

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
            fhirStore.Add(entry);

            Entry result = fhirStore.Get(entry.Key);
            transfer.Externalize(result);

            return Respond.WithResource(current != null ? HttpStatusCode.OK : HttpStatusCode.Created, result);
        }

        public FhirResponse ConditionalCreate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> query)
        {
            throw new NotImplementedException();
        }

        public FhirResponse Everything(Key key)
        {
            var searchCommand = new SearchParams();
            searchCommand.Add("_id", key.ResourceId);
            var compartment = fhirModel.FindCompartmentInfo(key.TypeName);
            if (compartment != null)
            {
                foreach (var ri in compartment.ReverseIncludes)
                {
                    searchCommand.RevInclude.Add(ri);
                }
            }
            return Search(key.TypeName, searchCommand);
        }

        public FhirResponse Document(Key key)
        {
            if (key.TypeName != fhirModel.GetResourceNameForResourceType(ResourceType.Composition))
            {
                throw new ArgumentException(String.Format("Document operation is only valid for Composition, not for {0}.", key.TypeName));
            }

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
            Entry current = fhirStore.Get(key);
            Validate.IsSameVersion(current.Key, versionedkey);

            return this.Put(key, resource);
        }

        public FhirResponse Update(IKey key, Resource resource)
        {
            return key.HasVersionId() ? this.VersionSpecificUpdate(key, resource)
                : this.Put(key, resource);
        }

        //why do I receive a key? and not just a type
        public FhirResponse ConditionalUpdate(Key key, Resource resource, SearchParams _params)
        {
            //if update receives a key with no version how do we handle concurrency?
            ISearchExtension searchExtension = fhirStore.FindExtension<ISearchExtension>();
            if (searchExtension == null)
                throw new NotSupportedException("Operation not supported");

            Key existing = searchExtension.FindSingle(key.TypeName, _params).WithoutVersion();
            return this.Update(existing, resource);
        }

        public FhirResponse Delete(IKey key)
        {
            Validate.Key(key);
            Validate.HasNoVersion(key);

            Entry current = fhirStore.Get(key);

            if (current != null && current.IsPresent)
            {
                Entry entry = Entry.DELETE(key, DateTimeOffset.UtcNow);
                transfer.Internalize(entry);
                fhirStore.Add(entry);
            }
            return Respond.WithCode(HttpStatusCode.NoContent);
        }

        public FhirResponse ConditionalDelete(Key key, IEnumerable<Tuple<string, string>> parameters)
        {
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

        public FhirResponse ValidateOperation(Key key, Resource resource)
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

        public FhirResponse Search(string type, SearchParams searchCommand)
        {
            ISearchExtension searchExtension = fhirStore.FindExtension<ISearchExtension>();
            //TODO: return 501 - 	Requested HTTP operation not supported?
            if (searchExtension == null)
                throw new NotSupportedException("Operation not supported");

            Snapshot snapshot = searchExtension.GetSnapshot(type, searchCommand);

            return CreateSnapshotResponse(snapshot);
        }

        private FhirResponse CreateSnapshotResponse(Snapshot snapshot)
        {
            IPagingExtension pagingExtension = fhirStore.FindExtension<IPagingExtension>();
            if (pagingExtension == null)
            {
                IList<Entry> results = fhirStore.Get(snapshot.Keys, null);
                transfer.Externalize(results);
                Bundle bundle = new Bundle()
                {
                    Type = snapshot.Type,
                    Total = snapshot.Count
                };
                bundle.Append(results);
                return responseFactory.GetFhirResponse(bundle);
            }
            else
            {
                Bundle bundle = pagingExtension.CreatePagination(snapshot).GetPage(0);
                transfer.Externalize(bundle);
                return responseFactory.GetFhirResponse(bundle);
            }
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

            return responseFactory.GetFhirResponse(interactions, Bundle.BundleType.TransactionResponse);
        }
        
        public FhirResponse Transaction(Bundle bundle)
        {
            throw new NotImplementedException();
        }

        public FhirResponse History(HistoryParameters parameters)
        {
         
            IHistoryExtension historyExtension = fhirStore.FindExtension<IHistoryExtension>();
            if (historyExtension == null)
                throw new NotSupportedException("Operation not supported");
        
            return CreateSnapshotResponse(historyExtension.History(parameters));
        }

        public FhirResponse History(string type, HistoryParameters parameters)
        {
            IHistoryExtension historyExtension = fhirStore.FindExtension<IHistoryExtension>();
            if (historyExtension == null)
                throw new NotSupportedException("Operation not supported");

            return CreateSnapshotResponse(historyExtension.History(type, parameters));
        }

        public FhirResponse History(Key key, HistoryParameters parameters)
        {
            IHistoryExtension historyExtension = fhirStore.FindExtension<IHistoryExtension>();
            if (historyExtension == null)
                throw new NotSupportedException("Operation not supported");

            return CreateSnapshotResponse(historyExtension.History(key, parameters));
        }

        public FhirResponse Mailbox(Bundle bundle, Binary body)
        {
            throw new NotImplementedException();
        }

        public FhirResponse Conformance(string sparkVersion)
        {
            return Respond.WithResource(ConformanceBuilder.GetSparkConformance(sparkVersion, transfer));
        }
        public FhirResponse GetPage(string snapshotkey, int index)
        {
            IPagingExtension pagingExtension = fhirStore.FindExtension<IPagingExtension>();
            if (pagingExtension == null)
                throw new NotSupportedException("Operation not supported");

            return responseFactory.GetFhirResponse(pagingExtension.CreatePagination(snapshotkey).GetPage(index));
        }
        private static void ValidateKey(Key key, bool withVersion = false)
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
    }
}