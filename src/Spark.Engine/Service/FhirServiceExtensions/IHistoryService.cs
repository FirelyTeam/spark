using System;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    internal interface IHistoryService : IFhirServiceExtension
    {
        [Obsolete("Use Async method version instead")]
        Snapshot History(string typename, HistoryParameters parameters);

        [Obsolete("Use Async method version instead")]
        Snapshot History(IKey key, HistoryParameters parameters);

        [Obsolete("Use Async method version instead")]
        Snapshot History(HistoryParameters parameters);

        Task<Snapshot> HistoryAsync(string typename, HistoryParameters parameters);

        Task<Snapshot> HistoryAsync(IKey key, HistoryParameters parameters);

        Task<Snapshot> HistoryAsync(HistoryParameters parameters);
    }
}