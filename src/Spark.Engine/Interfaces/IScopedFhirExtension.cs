namespace Spark.Engine.Interfaces
{
    public interface IScopedFhirExtension<T> : IFhirStoreExtension
    {
        T Scope { set; }
    }
}