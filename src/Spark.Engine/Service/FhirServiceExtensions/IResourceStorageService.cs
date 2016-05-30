using System.Collections.Generic;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IResourceStorageService : IFhirServiceExtension
    {
        Entry Get(IKey key);
        Entry Add(Entry entry);
        IList<Entry> Get(IEnumerable<string> localIdentifiers, string sortby = null);
    }
}