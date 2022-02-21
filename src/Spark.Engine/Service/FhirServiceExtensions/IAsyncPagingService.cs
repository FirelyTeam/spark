using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IAsyncPagingService : IFhirServiceExtension
    {
        Task<IAsyncSnapshotPagination> StartPaginationAsync(Snapshot snapshot);

        Task<IAsyncSnapshotPagination> StartPaginationAsync(string snapshotKey);
    }
}
