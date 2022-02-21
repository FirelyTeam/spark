using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IPagingService : IFhirServiceExtension
    {
        ISnapshotPagination StartPagination(Snapshot snapshot);
        ISnapshotPagination StartPagination(string snapshotkey);
    }
}
