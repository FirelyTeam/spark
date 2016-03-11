using Spark.Core;

namespace Spark.Engine.Interfaces
{
    public interface IScopedGenerator<T> : IGenerator
    {
         T Scope { get; set; }
    }
}