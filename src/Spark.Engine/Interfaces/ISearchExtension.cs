using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Engine.Interfaces
{
    public interface ISearchExtension : IFhirStoreExtension
    {
        Snapshot GetSnapshot(string type, SearchParams searchCommand);
        IKey FindSingle(string type, SearchParams searchCommand);
    }
}