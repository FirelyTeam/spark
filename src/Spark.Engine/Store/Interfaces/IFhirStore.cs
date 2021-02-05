using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface IFhirStore
    {
        [Obsolete("Use Async method version instead")]
        void Add(Entry entry);

        [Obsolete("Use Async method version instead")]
        Entry Get(IKey key);

        [Obsolete("Use Async method version instead")]
        IList<Entry> Get(IEnumerable<IKey> localIdentifiers);

        Task AddAsync(Entry entry);

        Task<Entry> GetAsync(IKey key);

        Task<IList<Entry>> GetAsync(IEnumerable<IKey> localIdentifiers);
    }
}