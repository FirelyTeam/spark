using System.Collections.Generic;
using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface IFhirStore
    {
        void Add(Entry entry);
        Entry Get(IKey key);
        IList<Entry> Get(IEnumerable<IKey> localIdentifiers);
    }
   
}