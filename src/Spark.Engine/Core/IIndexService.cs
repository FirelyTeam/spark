using System;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Spark.Engine.Model;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Core
{
    public interface IIndexService
    {
        [Obsolete("Use ProcessAsync(Entry) instead")]
        void Process(Entry entry);

        [Obsolete("Use IndexResourceAsync(Resource, IKey) instead")]
        IndexValue IndexResource(Resource resource, IKey key);

        Task ProcessAsync(Entry entry);

        Task<IndexValue> IndexResourceAsync(Resource resource, IKey key);
    }
}
