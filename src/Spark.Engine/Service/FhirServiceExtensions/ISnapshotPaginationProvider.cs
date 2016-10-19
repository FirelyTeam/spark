using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ISnapshotPaginationProvider
    {
        ISnapshotPagination StartPagination(Snapshot snapshot);
    }
}