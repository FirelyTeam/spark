using Spark.Engine.Core;
using Spark.Engine.Model;

namespace Spark.Engine.Store.Interfaces
{
    using System.Threading.Tasks;

    public interface IIndexStore
    {
        Task Save(IndexValue indexValue);

        Task Delete(Entry entry);

        Task Clean();
    }
}
