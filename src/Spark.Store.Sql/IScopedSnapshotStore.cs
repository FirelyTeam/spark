using Spark.Core;

namespace Spark.Store.Sql
{
    internal interface IScopedSnapshotStore : ISnapshotStore
    {
        IScope Scope { set; }
    }
}