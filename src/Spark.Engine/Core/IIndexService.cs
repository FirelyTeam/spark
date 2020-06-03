using Hl7.Fhir.Model;
using Spark.Engine.Model;

namespace Spark.Engine.Core
{
    public interface IIndexService
    {
        System.Threading.Tasks.Task Process(Entry entry);
        System.Threading.Tasks.Task<IndexValue> IndexResource(Resource resource, IKey key);
    }
}
