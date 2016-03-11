using Spark.Engine.Core;
using Spark.Engine.Service;
using Spark.Engine.Service.Extensions;

namespace Spark.Store.Sql.StoreExtensions
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