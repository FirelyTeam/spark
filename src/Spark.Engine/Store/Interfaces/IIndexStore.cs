using System;
using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Model;

namespace Spark.Engine.Store.Interfaces
{
    public interface IIndexStore
    {
        [Obsolete("Use SaveAsync(IndexValue) instead")]
        void Save(IndexValue indexValue);

        [Obsolete("Use DeleteAsync(Entry) instead")]
        void Delete(Entry entry);

        [Obsolete("Use CleanAsync() instead")]
        void Clean();

        Task SaveAsync(IndexValue indexValue);

        Task DeleteAsync(Entry entry);

        Task CleanAsync();
    }
}
