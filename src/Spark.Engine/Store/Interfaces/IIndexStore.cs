using System;
using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Model;

namespace Spark.Engine.Store.Interfaces
{
    public interface IIndexStore
    {
        [Obsolete("Use Async method version instead")]
        void Save(IndexValue indexValue);

        [Obsolete("Use Async method version instead")]
        void Delete(Entry entry);

        [Obsolete("Use Async method version instead")]
        void Clean();

        Task SaveAsync(IndexValue indexValue);

        Task DeleteAsync(Entry entry);

        Task CleanAsync();
    }
}
