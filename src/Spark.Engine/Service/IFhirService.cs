using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Service
{
    using System.Threading.Tasks;

    public interface IFhirService
    {
        Task<FhirResponse> AddMeta(IKey key, Parameters parameters);
        Task<FhirResponse> ConditionalCreate(IKey key, Resource resource, SearchParams parameters);
        Task<FhirResponse> ConditionalDelete(IKey key, IEnumerable<Tuple<string, string>> parameters);
        Task<FhirResponse> ConditionalUpdate(IKey key, Resource resource, SearchParams _params);
        FhirResponse Conformance(string sparkVersion);
        Task<FhirResponse> Create(IKey key, Resource resource);
        Task<FhirResponse> Delete(IKey key);
        Task<FhirResponse> GetPage(string snapshotkey, int index);
        Task<FhirResponse> History(HistoryParameters parameters);
        Task<FhirResponse> History(string type, HistoryParameters parameters);
        Task<FhirResponse> History(IKey key, HistoryParameters parameters);
        Task<FhirResponse> Mailbox(Bundle bundle, Binary body);
        Task<FhirResponse> Put(IKey key, Resource resource);
        Task<FhirResponse> Put(Entry entry);
        Task<FhirResponse> Read(IKey key, ConditionalHeaderParameters parameters = null);
        Task<FhirResponse> ReadMeta(IKey key);
        Task<FhirResponse> Search(string type, SearchParams searchCommand, int pageIndex = 0);
        Task<FhirResponse> Transaction(Bundle bundle);
        Task<FhirResponse> Update(IKey key, Resource resource);
        FhirResponse ValidateOperation(IKey key, Resource resource);
        Task<FhirResponse> VersionRead(IKey key);
        Task<FhirResponse> VersionSpecificUpdate(IKey versionedkey, Resource resource);
        Task<FhirResponse> Everything(IKey key);
        Task<FhirResponse> Document(IKey key);
    }
}