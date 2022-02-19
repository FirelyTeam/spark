using System.Collections.Generic;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface IAsyncFhirStore
    {
        Task AddAsync(Entry entry);

        Task<Entry> GetAsync(IKey key);

        Task<IList<Entry>> GetAsync(IEnumerable<IKey> localIdentifiers);
    }
}
