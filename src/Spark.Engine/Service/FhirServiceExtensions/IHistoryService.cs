using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    internal interface IHistoryService : IFhirServiceExtension
    {
        Snapshot History(string typename, HistoryParameters parameters);
        Snapshot History(IKey key, HistoryParameters parameters);
        Snapshot History(HistoryParameters parameters);
    }
}