using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IAsyncSnapshotPaginationProvider
    {
        IAsyncSnapshotPagination StartAsyncPagination(Snapshot snapshot);
    }
}
