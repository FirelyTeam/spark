using System;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Spark.Engine.Model;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Core
{
    public interface IIndexService
    {
        [Obsolete("Use Async method version instead")]
        void Process(Entry entry);

        [Obsolete("Use Async method version instead")]
        IndexValue IndexResource(Resource resource, IKey key);

        Task ProcessAsync(Entry entry);

        Task<IndexValue> IndexResourceAsync(Resource resource, IKey key);
    }
}
