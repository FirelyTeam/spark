using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    using System.Threading.Tasks;

    public interface IPagingService : IFhirServiceExtension
    {
        Task<ISnapshotPagination> StartPagination(Snapshot snapshot);
        Task<ISnapshotPagination> StartPagination(string snapshotkey);
    }
}