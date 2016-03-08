namespace Spark.Engine.Interfaces
{
    public interface IScopedFhirStore<T> : IBaseFhirStore
    {
         T Scope { get; set; }
    }
}