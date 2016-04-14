using Spark.Engine.Store.Interfaces;

namespace Spark.Store.Sql
{
    internal interface IScopedSnapshotStore : ISnapshotStore
    {
        IScope Scope { set; }
    }
}