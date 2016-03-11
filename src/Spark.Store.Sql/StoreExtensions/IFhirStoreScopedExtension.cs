using Spark.Engine.Interfaces;

namespace Spark.Store.Sql.StoreExtensions
{
    public interface IFhirStoreScopedExtension<T> : IFhirStoreExtension
    {
        T Scope { get; set; }
    }
}