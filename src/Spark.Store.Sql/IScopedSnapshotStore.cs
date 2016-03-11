using Spark.Core;

namespace Spark.Store.Sql
{
    public interface IScopedSnapshotStore<T> : ISnapshotStore
        where T : IScope
    {
        T Scope { set; }
    }
}