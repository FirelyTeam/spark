using Spark.Core;

namespace Spark.Store.Sql
{
    public interface IScopedFhirIndex<T> : IFhirIndex
        where T:IScope
    {
         T Scope { set; }
    }
}