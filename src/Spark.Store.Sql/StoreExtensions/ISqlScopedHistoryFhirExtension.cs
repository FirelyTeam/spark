using Spark.Engine.Interfaces;

namespace Spark.Store.Sql.StoreExtensions
{
    public interface ISqlScopedHistoryFhirExtension<T> : IScopedFhirExtension<T>, IHistoryExtension
    {
         
    }
}