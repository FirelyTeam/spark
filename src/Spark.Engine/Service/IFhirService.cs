using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Service
{
    public interface IFhirService
    {
        FhirResponse AddMeta(Key key, Parameters parameters);
        FhirResponse ConditionalCreate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> query);
        FhirResponse ConditionalDelete(Key key, IEnumerable<Tuple<string, string>> parameters);
        FhirResponse ConditionalUpdate(Key key, Resource resource, SearchParams _params);
        FhirResponse Conformance(string sparkVersion);
        FhirResponse Create(IKey key, Resource resource);
        FhirResponse Delete(IKey key);
        FhirResponse GetPage(string snapshotkey, int index);
        FhirResponse HandleInteraction(Entry interaction);
        FhirResponse History(HistoryParameters parameters);
        FhirResponse History(string type, HistoryParameters parameters);
        FhirResponse History(Key key, HistoryParameters parameters);
        FhirResponse Mailbox(Bundle bundle, Binary body);
        FhirResponse Put(IKey key, Resource resource);
        FhirResponse Read(Key key, ConditionalHeaderParameters parameters = null);
        FhirResponse ReadMeta(Key key);
        FhirResponse Search(string type, SearchParams searchCommand);
        FhirResponse Transaction(IList<Entry> interactions);
        FhirResponse Transaction(Bundle bundle);
        FhirResponse Update(IKey key, Resource resource);
        FhirResponse ValidateOperation(Key key, Resource resource);
        FhirResponse VersionRead(Key key);
        FhirResponse VersionSpecificUpdate(IKey versionedkey, Resource resource);
    }
}