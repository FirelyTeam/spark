using System.Collections.Generic;
using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    using System.Threading.Tasks;

    public interface IFhirStore
    {
        Task Add(Entry entry);
        Task<Entry> Get(IKey key);
        Task<IList<Entry>> Get(IEnumerable<IKey> localIdentifiers);
    }
}