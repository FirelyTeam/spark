using System.Collections.Generic;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IResourceStorageService : IFhirServiceExtension
    {
        Task<Entry> Get(IKey key);
        Task<Entry> Add(Entry entry);
        Task<IList<Entry>> Get(IEnumerable<string> localIdentifiers, string sortby = null);
    }
}
