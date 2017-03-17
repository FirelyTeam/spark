using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Service
{
    public interface IFhirService
    {
        FhirResponse AddMeta(IKey key, Parameters parameters);
        FhirResponse ConditionalCreate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> parameters);
        FhirResponse ConditionalCreate(IKey key, Resource resource, SearchParams parameters);
        FhirResponse ConditionalDelete(IKey key, IEnumerable<Tuple<string, string>> parameters);
        FhirResponse ConditionalUpdate(IKey key, Resource resource, SearchParams _params);
        FhirResponse Conformance(string sparkVersion);
        FhirResponse Create(IKey key, Resource resource);
        FhirResponse Delete(IKey key);
        FhirResponse Delete(Entry entry);
        FhirResponse GetPage(string snapshotkey, int index);
        FhirResponse History(HistoryParameters parameters);
        FhirResponse History(string type, HistoryParameters parameters);
        FhirResponse History(IKey key, HistoryParameters parameters);
        FhirResponse Mailbox(Bundle bundle, Binary body);
        FhirResponse Put(IKey key, Resource resource);
        FhirResponse Put(Entry entry);
        FhirResponse Read(IKey key, ConditionalHeaderParameters parameters = null);
        FhirResponse ReadMeta(IKey key);
        FhirResponse Search(string type, SearchParams searchCommand, int pageIndex = 0);
        FhirResponse Transaction(IList<Entry> interactions);
        FhirResponse Transaction(Bundle bundle);
        FhirResponse Update(IKey key, Resource resource);
        FhirResponse ValidateOperation(IKey key, Resource resource);
        FhirResponse VersionRead(IKey key);
        FhirResponse VersionSpecificUpdate(IKey versionedkey, Resource resource);
        FhirResponse Everything(IKey key);
        FhirResponse Document(IKey key);
    }
}