using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ISearchService : IFhirServiceExtension
    {
        Snapshot GetSnapshot(string type, SearchParams searchCommand);
        Snapshot GetSnapshotForEverything(IKey key);
        IKey FindSingle(string type, SearchParams searchCommand);
    }
}