using Spark.Engine.Interfaces;

namespace Spark.Store.Sql
{
    public interface ISqlScopedSearchFhirExtension<T> : IScopedFhirExtension<T>, ISearchExtension
           where T : IScope
    {
         
    }
}