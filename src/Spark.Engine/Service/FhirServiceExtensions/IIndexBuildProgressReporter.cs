using System.Threading.Tasks;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IIndexBuildProgressReporter
    {
        Task ReportProgressAsync(int progress, string message);

        Task ReportErrorAsync(string message);
    }
}