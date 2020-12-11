using Hl7.Fhir.Model;
using Spark.Engine.Model;

namespace Spark.Engine.Core
{
    using System.Threading.Tasks;

    public interface IIndexService
    {
        Task Process(Entry entry);
        Task<IndexValue> IndexResource(Resource resource, IKey key);
    }
}