using Spark.Engine.Core;
using Spark.Engine.Model;

namespace Spark.Engine.Store.Interfaces
{
    public interface IIndexStore
    {
        void Save(IndexValue indexValue);

        void Delete(Entry entry);

        void Clean();
    }
}
