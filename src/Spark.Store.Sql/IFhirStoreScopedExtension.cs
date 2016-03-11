using Spark.Engine.Interfaces;

namespace Spark.Store.Sql
{
    public interface IFhirStoreScopedExtension<T> : IFhirStoreExtension
    {
        T Scope { get; set; }
    }
}