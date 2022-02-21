using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Model;

namespace Spark.Engine.Store.Interfaces
{
    public interface IAsyncIndexStore
    {
        Task SaveAsync(IndexValue indexValue);

        Task DeleteAsync(Entry entry);

        Task CleanAsync();
    }
}
