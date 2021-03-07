using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IResourceStorageService : IFhirServiceExtension
    {
        [Obsolete("Use GetAsync(IKey) instead")]
        Entry Get(IKey key);
        [Obsolete("Use AddAsync(Entry) instead")]
        Entry Add(Entry entry);
        [Obsolete("Use GetAsync(IKey, string) instead")]
        IList<Entry> Get(IEnumerable<string> localIdentifiers, string sortby = null);
        Task<Entry> GetAsync(IKey key);
        Task<Entry> AddAsync(Entry entry);
        Task<IList<Entry>> GetAsync(IEnumerable<string> localIdentifiers, string sortby = null);
    }
}