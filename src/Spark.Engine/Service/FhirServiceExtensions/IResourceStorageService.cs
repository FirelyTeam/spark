using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IResourceStorageService : IFhirServiceExtension
    {
        [Obsolete("Use Async method version instead")]
        Entry Get(IKey key);

        [Obsolete("Use Async method version instead")]
        Entry Add(Entry entry);

        [Obsolete("Use Async method version instead")]
        IList<Entry> Get(IEnumerable<string> localIdentifiers, string sortby = null);

        Task<Entry> GetAsync(IKey key);

        Task<Entry> AddAsync(Entry entry);

        Task<IList<Entry>> GetAsync(IEnumerable<string> localIdentifiers, string sortby = null);
    }
}