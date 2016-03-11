namespace Spark.Engine.Interfaces
{
    public interface IScopedFhirStore<T> : IFhirStore
    {
         T Scope { get; set; }
    }
}