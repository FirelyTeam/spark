using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    using System.Threading.Tasks;

    public interface IPagingService : IFhirServiceExtension
    {
        Task<ISnapshotPagination> StartPagination(Snapshot snapshot);
        Task<ISnapshotPagination> StartPagination(string snapshotkey);
    }
}