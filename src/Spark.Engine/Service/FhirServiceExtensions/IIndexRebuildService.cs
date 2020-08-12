using System;
using System.Threading.Tasks;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IIndexRebuildService
    {
        Task RebuildIndexAsync(Func<int, string, Task> progressAction = null);
    }
}
