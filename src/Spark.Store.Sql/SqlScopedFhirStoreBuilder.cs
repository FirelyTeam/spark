using System;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Search;
using Spark.Engine.Service;
using Spark.Engine.Service.Extensions;
using Spark.Service;
using Spark.Store.Sql.StoreExtensions;

namespace Spark.Store.Sql
{
    public class SqlScopedFhirStoreBuilder : IScopedFhirStoreBuilder
    {
        public IScopedFhirStore<T> BuildStore<T>(Uri baseUri, Func<T, int> scopeKeyProvider)
        {
            SqlScopedFhirStore<T> store = new SqlScopedFhirStore<T>(new FormatId(), scopeKeyProvider);
            store.AddExtension(new SqlScopedSearchFhirExtension(
                new IndexService(new FhirModel(), new FhirPropertyIndex(new FhirModel()),
                    new ResourceVisitor(new FhirPropertyIndex(new FhirModel())), new ElementIndexer(new FhirModel()),
                    new SqlScopedIndexStore()),
                new SqlScopedFhirIndex(new FormatId()), new Localhost(baseUri), new SqlScopedSnapshotStore()));
            store.AddExtension(new SqlScopedHistoryFhirExtension());
            ((IFhirStore)store).AddExtension(new PagingExtension(new SqlScopedSnapshotStore(), new Transfer(store, new Localhost(baseUri)), new Localhost(baseUri)));

            return store;
        }
    }


    public class SqlScopedFhirServiceFactory : GenericScopedFhirServiceFactory
    {
        public SqlScopedFhirServiceFactory() : base(new SqlScopedFhirStoreBuilder())
        {
        }
    }
}