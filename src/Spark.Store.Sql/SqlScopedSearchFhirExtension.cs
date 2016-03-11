using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Service;

namespace Spark.Store.Sql
{
    public class SqlScopedSearchFhirExtension<T> : SearchExtension, ISqlScopedSearchFhirExtension<T>
         where T : IScope
    {
        private readonly IScopedFhirIndex<T> fhirIndex;
        private readonly IScopedSnapshotStore<T> snapshotStore;

        public T Scope
        {
            set
            {
                fhirIndex.Scope = value;
                snapshotStore.Scope = value;
            }
        }

        public SqlScopedSearchFhirExtension(IndexService indexService, IScopedFhirIndex<T> fhirIndex, ILocalhost localhost, IScopedSnapshotStore<T> snapshotStore) 
            : base(indexService, fhirIndex, localhost, snapshotStore)
        {
            this.fhirIndex = fhirIndex;
            this.snapshotStore = snapshotStore;
        }
    }
}