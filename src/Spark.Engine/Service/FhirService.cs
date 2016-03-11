using System;
using System.Collections.Generic;
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Core;
using Spark.Engine.Auxiliary;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service
{
    public class FhirService : IFhirService
    {
        private readonly IBaseFhirStore fhirStore;
        private readonly IBaseFhirResponseFactory responseFactory;
        private readonly ITransfer transfer;

        internal FhirService(IBaseFhirStore fhirStore, IBaseFhirResponseFactory responseFactory, ITransfer transfer)
        {
            this.fhirStore = fhirStore;
            this.responseFactory = responseFactory;
            this.transfer = transfer;


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
            if (key.HasVersionId())
            {
                return this.VersionSpecificUpdate(key, resource);
            }
            else
            {
                return this.Put(key, resource);
            }
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
            //_log.ServiceMethodCalled("search");

            //Validate.TypeName(type);
            //SearchResults results = fhirIndex.Search(type, searchCommand);

            //if (results.HasErrors)
            //{
            //    throw new SparkException(HttpStatusCode.BadRequest, results.Outcome);
            //}

            //UriBuilder builder = new UriBuilder(localhost.Uri(type));
            //builder.Query = results.UsedParameters;
            //Uri link = builder.Uri;

            //var snapshot = pager.CreateSnapshot(link, results, searchCommand);
            //Bundle bundle = pager.GetFirstPage(snapshot);

            //if (results.HasIssues)
            //{
            //    bundle.AddResourceEntry(results.Outcome, new Uri("outcome/1", UriKind.Relative).ToString());
            //}

            //return Respond.WithBundle(bundle);
            ISearchExtension searchExtension = fhirStore.FindExtension<ISearchExtension>();
            if (searchExtension == null)
                throw new NotSupportedException("Operation not supported");

            Snapshot snapshot = searchExtension.GetSnapshot(type, searchCommand);

            IPagingExtension pagingExtension = fhirStore.FindExtension<IPagingExtension>();
            if (pagingExtension == null)
            {
                IList<Entry> results = fhirStore.GetCurrent(snapshot.Keys, null);
                transfer.Externalize(results);
                Bundle bundle = new Bundle();
                bundle.Append(results);
                return responseFactory.GetFhirResponse(bundle);
            }
            else
            {
                return responseFactory.GetFhirResponse(pagingExtension.GetPage(snapshot.Id, 0));
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
            //var since = parameters.Since ?? DateTimeOffset.MinValue;
            //Uri link = localhost.Uri(RestOperation.HISTORY);

            IHistoryExtension historyExtension = fhirStore.FindExtension<IHistoryExtension>();
            if (historyExtension == null)
                throw new NotSupportedException("Operation not supported");

            //IEnumerable<string> keys = fhirStore.History(since);
            //var snapshot = pager.CreateSnapshot(Bundle.BundleType.History, link, keys, parameters.SortBy, parameters.Count, null);
            //Bundle bundle = pager.GetFirstPage(snapshot);

            //// DSTU2: export
            //// exporter.Externalize(bundle);
            //return Respond.WithBundle(bundle);
            return responseFactory.GetFhirResponse(historyExtension.History(parameters));

        }

        public FhirResponse History(string type, HistoryParameters parameters)
        {
            IHistoryExtension historyExtension = fhirStore.FindExtension<IHistoryExtension>();
            if (historyExtension == null)
                throw new NotSupportedException("Operation not supported");

            return responseFactory.GetFhirResponse(historyExtension.History(type, parameters));
        }

        public FhirResponse History(Key key, HistoryParameters parameters)
        {
            IHistoryExtension historyExtension = fhirStore.FindExtension<IHistoryExtension>();
            if (historyExtension == null)
                throw new NotSupportedException("Operation not supported");

            return responseFactory.GetFhirResponse(historyExtension.History(key, parameters));
        }

        public FhirResponse Mailbox(Bundle bundle, Binary body)
        {
            throw new NotImplementedException();
        }

        public FhirResponse Conformance()
        {
            throw new NotImplementedException();
        }

        public FhirResponse GetPage(string snapshotkey, int index)
        {
            IPagingExtension pagingExtension = fhirStore.FindExtension<IPagingExtension>();
            if (pagingExtension == null)
                throw new NotSupportedException("Operation not supported");

            return responseFactory.GetFhirResponse(pagingExtension.GetPage(snapshotkey, index));
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