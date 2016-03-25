using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Interfaces
{
    public interface IHistoryExtension : IFhirStoreExtension
    {
        Snapshot History(string typename, HistoryParameters parameters);
        Snapshot History(IKey key, HistoryParameters parameters);
        Snapshot History(HistoryParameters parameters);
    }
}