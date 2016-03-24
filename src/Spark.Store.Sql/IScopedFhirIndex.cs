using Spark.Core;

namespace Spark.Store.Sql
{
    internal interface IScopedFhirIndex : IFhirIndex
    {
        IScope Scope { set; }
    }
}