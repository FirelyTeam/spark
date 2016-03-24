using Spark.Engine.Core;
using Spark.Engine.Service;
using Spark.Engine.Service.Extensions;

namespace Spark.Store.Sql.StoreExtensions
{
    internal class SqlScopedSearchFhirExtension : SearchExtension, IScopedFhirExtension
    {
        private readonly IScopedFhirIndex fhirIndex;
        private readonly IScopedSnapshotStore snapshotStore;

        public IScope Scope
        {
            set
            {
                fhirIndex.Scope = value;
                snapshotStore.Scope = value;
            }
        }

        public SqlScopedSearchFhirExtension(IndexService indexService, IScopedFhirIndex fhirIndex, ILocalhost localhost, IScopedSnapshotStore snapshotStore) 
            : base(indexService, fhirIndex, localhost, snapshotStore)
        {
            this.fhirIndex = fhirIndex;
            this.snapshotStore = snapshotStore;
        }
    }
}