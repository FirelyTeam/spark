using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Spark.Core;
using Spark.Engine.Auxiliary;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Store.Sql.Model;
using Resource = Spark.Store.Sql.Model.Resource;

namespace Spark.Store.Sql.StoreExtensions
{
    internal class SqlScopedHistoryFhirExtension : IScopedFhirExtension, IHistoryExtension
    {
        private readonly ISnapshotStore snapshotStore;
        private readonly ILocalhost localhost;
        public IScope Scope { get; set; }
        private readonly IFhirDbContext context;
        public const int MAX_PAGE_SIZE = 100;

        public SqlScopedHistoryFhirExtension(ISnapshotStore snapshotStore, ILocalhost localhost, IFhirDbContext context)
        {
            this.snapshotStore = snapshotStore;
            this.localhost = localhost;
            this.context = context;
        }

        public Snapshot History(HistoryParameters parameters)
        {
            var results = RestrictToScope(context.Resources);
            IEnumerable<int> ids = RestrictDate(results, parameters).Select(r => r.Id);
            Uri link = localhost.Uri(RestOperation.HISTORY);
            return CreateSnapshot(link, ids.Select(r => r.ToString()), parameters);
        }

        public Snapshot History(IKey key, HistoryParameters parameters)
        {
            var results = RestrictToScope(context.Resources).Where(
                r => (r.ResourceType == key.TypeName) && (r.Endpoint == key.ResourceId));
            IEnumerable<int> ids = RestrictDate(results, parameters).Select(r => r.Id);
            Uri link = localhost.Uri(key);
            return CreateSnapshot(link, ids.Select(r => r.ToString()), parameters);
        }

        public Snapshot History(string typeName, HistoryParameters parameters)
        {
            var results = RestrictToScope(context.Resources).Where(r => r.ResourceType == typeName);
            IEnumerable<int> ids = RestrictDate(results, parameters).Select(r => r.Id);
            Uri link = localhost.Uri(typeName, RestOperation.HISTORY);
            return CreateSnapshot(link, ids.Select(r => r.ToString()), parameters);
        }

        private IQueryable<Resource> RestrictToScope(IQueryable<Resource> queryable)
        {
            if (Scope != null)
            {
                return queryable.Where(r => r.ScopeKey == Scope.ScopeKey);
            }
            return queryable;
        }

        private IQueryable<Resource> RestrictDate(IQueryable<Resource> queryable, HistoryParameters parameters)
        {
            if (parameters.Since != null)
            {
               return queryable.Where(r => r.CreationDate > parameters.Since);
            }
            return queryable;
        }

        public void OnEntryAdded(Entry entry)
        {
        }

        public void OnExtensionAdded(IFhirStore extensibleObject)
        {
        }

        private Snapshot CreateSnapshot(Uri selflink, IEnumerable<string> keys, HistoryParameters historyParameters)
        {
            Snapshot historySnapshot = Snapshot.Create(Bundle.BundleType.Searchset, selflink, keys, historyParameters.SortBy, NormalizeCount(historyParameters.Count), null);
            snapshotStore.AddSnapshot(historySnapshot);
            return historySnapshot;
        }

        private int? NormalizeCount(int? count)
        {
            if (count.HasValue)
            {
                return Math.Min(count.Value, MAX_PAGE_SIZE);
            }
            return count;
        }

    }
}