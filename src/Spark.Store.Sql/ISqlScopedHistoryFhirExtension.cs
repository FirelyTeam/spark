using Spark.Engine.Interfaces;

namespace Spark.Store.Sql
{
    public interface ISqlScopedHistoryFhirExtension<T> : IScopedFhirExtension<T>, IHistoryExtension
    {
         
    }
}