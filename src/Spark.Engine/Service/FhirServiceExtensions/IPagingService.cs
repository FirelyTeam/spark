using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IPagingService : IFhirServiceExtension
    {
        ISnapshotPagination StartPagination(Snapshot snapshot);
        ISnapshotPagination StartPagination(string snapshotkey);

        Task<ISnapshotPagination> StartPaginationAsync(Snapshot snapshot);
        Task<ISnapshotPagination> StartPaginationAsync(string snapshotKey);
    }
}