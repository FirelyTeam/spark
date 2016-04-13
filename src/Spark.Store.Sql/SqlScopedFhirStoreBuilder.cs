using System;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Search;
using Spark.Engine.Service;
using Spark.Engine.Service.Extensions;
using Spark.Service;
using Spark.Store.Sql.Model;
using Spark.Store.Sql.StoreExtensions;

namespace Spark.Store.Sql
{
    public class SqlScopedFhirStoreBuilder : IScopedFhirStoreBuilder
    {
        private readonly IFhirDbContext _context;

        public SqlScopedFhirStoreBuilder(IFhirDbContext context)
        {
            _context = context;
        }

        public IScopedFhirStore<T> BuildStore<T>(Uri baseUri, Func<T, int> scopeKeyProvider)
        {
            return BuildSqlStore(baseUri, scopeKeyProvider);
        }

        public SqlScopedFhirStore<T> BuildSqlStore<T>(Uri baseUri, Func<T, int> scopeKeyProvider)
        {
            SqlScopedFhirStore<T> store = new SqlScopedFhirStore<T>(new FormatId(), scopeKeyProvider, _context);
            store.AddExtension(new SqlScopedSearchFhirExtension(
                new IndexService(new FhirModel(), new FhirPropertyIndex(new FhirModel()),
                    new ResourceVisitor(new FhirPropertyIndex(new FhirModel())), new ElementIndexer(new FhirModel()),
                    new SqlScopedIndexStore()),
                new SqlScopedFhirIndex(new FormatId(), _context), new Localhost(baseUri), new SqlScopedSnapshotStore(_context)));
            store.AddExtension(new SqlScopedHistoryFhirExtension(new SqlScopedSnapshotStore(_context), new Localhost(baseUri), _context));
            ((IFhirStore)store).AddExtension(new PagingExtension(new SqlScopedSnapshotStore(_context), new Transfer(store, new Localhost(baseUri)), new Localhost(baseUri)));

            return store;
        }
    }

   
}