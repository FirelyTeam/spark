using System.Threading.Tasks;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IIndexRebuildService
    {
        Task RebuildIndexAsync(IIndexBuildProgressReporter reporter = null);
    }
}
