using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Service;
using System;
using System.Collections.Generic;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Service
{
    [Obsolete("Use AsyncFhirService instead")]
    public class FhirService : IFhirService
    {
        private readonly IAsyncFhirService _asyncFhirService;

        public FhirService(IAsyncFhirService asyncFhirService)
        {
            _asyncFhirService = asyncFhirService ?? throw new ArgumentNullException(nameof(asyncFhirService));
        }

        public FhirResponse Read(IKey key, ConditionalHeaderParameters parameters = null)
        {
            return Task.Run(() => _asyncFhirService.ReadAsync(key, parameters)).Result;
        }

        public FhirResponse ReadMeta(IKey key)
        {
            return Task.Run(() => _asyncFhirService.ReadMetaAsync(key)).Result;
        }

        public FhirResponse AddMeta(IKey key, Parameters parameters)
        {
            return Task.Run(() => _asyncFhirService.AddMetaAsync(key, parameters)).Result;
        }

        public FhirResponse VersionRead(IKey key)
        {
            return Task.Run(() => _asyncFhirService.VersionReadAsync(key)).Result;
        }

        public FhirResponse Create(IKey key, Resource resource)
        {
            return Task.Run(() => _asyncFhirService.CreateAsync(key, resource)).Result;
        }

        public FhirResponse Put(Entry entry)
        {
            return Task.Run(() => _asyncFhirService.PutAsync(entry)).Result;
        }

        public FhirResponse Put(IKey key, Resource resource)
        {
            return Task.Run(() => _asyncFhirService.PutAsync(key, resource)).Result;
        }

        public FhirResponse ConditionalCreate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> parameters)
        {
            return Task.Run(() => _asyncFhirService.ConditionalCreateAsync(key, resource, parameters)).Result;
        }

        public FhirResponse ConditionalCreate(IKey key, Resource resource, SearchParams parameters)
        {
            return Task.Run(() => _asyncFhirService.ConditionalCreateAsync(key, resource, parameters)).Result;
        }

        public FhirResponse Everything(IKey key)
        {
            return Task.Run(() => _asyncFhirService.EverythingAsync(key)).Result;
        }

        public FhirResponse Document(IKey key)
        {
            return Task.Run(() => _asyncFhirService.DocumentAsync(key)).Result;
        }

        public FhirResponse VersionSpecificUpdate(IKey versionedkey, Resource resource)
        {
            return Task.Run(() => _asyncFhirService.VersionSpecificUpdateAsync(versionedkey, resource)).Result;
        }

        public FhirResponse Update(IKey key, Resource resource)
        {
            return Task.Run(() => _asyncFhirService.UpdateAsync(key, resource)).Result;
        }

        public FhirResponse ConditionalUpdate(IKey key, Resource resource, SearchParams @params)
        {
            return Task.Run(() => _asyncFhirService.ConditionalUpdateAsync(key, resource, @params)).Result;
        }

        public FhirResponse Delete(IKey key)
        {
            return Task.Run(() => _asyncFhirService.DeleteAsync(key)).Result;
        }

        public FhirResponse Delete(Entry entry)
        {
            return Task.Run(() => _asyncFhirService.DeleteAsync(entry)).Result;
        }

        public FhirResponse ConditionalDelete(IKey key, IEnumerable<Tuple<string, string>> parameters)
        {
            return Task.Run(() => _asyncFhirService.ConditionalDeleteAsync(key, parameters)).Result;
        }

        public FhirResponse ValidateOperation(IKey key, Resource resource)
        {
            return Task.Run(() => _asyncFhirService.ValidateOperationAsync(key, resource)).Result;
        }

        public FhirResponse Search(string type, SearchParams searchCommand, int pageIndex = 0)
        {
            return Task.Run(() => _asyncFhirService.SearchAsync(type, searchCommand, pageIndex)).Result;
        }

        public FhirResponse Transaction(IList<Entry> interactions)
        {
            return Task.Run(() => _asyncFhirService.TransactionAsync(interactions)).Result;
        }

        public FhirResponse Transaction(Bundle bundle)
        {
            return Task.Run(() => _asyncFhirService.TransactionAsync(bundle)).Result;
        }

        public FhirResponse History(HistoryParameters parameters)
        {
            return Task.Run(() => _asyncFhirService.HistoryAsync(parameters)).Result;
        }

        public FhirResponse History(string type, HistoryParameters parameters)
        {
            return Task.Run(() => _asyncFhirService.HistoryAsync(type, parameters)).Result;
        }

        public FhirResponse History(IKey key, HistoryParameters parameters)
        {
            return Task.Run(() => _asyncFhirService.HistoryAsync(key, parameters)).Result;
        }

        public FhirResponse Mailbox(Bundle bundle, Binary body)
        {
            return Task.Run(() => _asyncFhirService.MailboxAsync(bundle, body)).Result;
        }

        public FhirResponse CapabilityStatement(string sparkVersion)
        {
            return Task.Run(() => _asyncFhirService.CapabilityStatementAsync(sparkVersion)).Result;
        }

        public FhirResponse GetPage(string snapshotkey, int index)
        {
            return Task.Run(() => _asyncFhirService.GetPageAsync(snapshotkey, index)).Result;
        }
    }
}
