using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Model;

namespace Spark.Engine.Store.Interfaces
{
    public interface IIndexStore
    {
        void Save(IndexValue indexValue);
        Task SaveAsync(IndexValue indexValue);
        void Delete(Entry entry);
        Task DeleteAsync(Entry entry);
        void Clean();
        Task CleanAsync();
    }
}
