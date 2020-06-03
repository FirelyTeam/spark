using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Model;

namespace Spark.Engine.Store.Interfaces
{
    public interface IIndexStore
    {
        Task Save(IndexValue indexValue);
        Task Delete(Entry entry);
        Task Clean();
    }
}
