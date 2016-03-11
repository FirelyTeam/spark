using System;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Search;
using Spark.Engine.Service;
using Spark.Service;

namespace Spark.Store.Sql
{
    public class SqlScopedFhirStoreBuilder<T> : IScopedFhirStoreBuilder<T> where T : IScope
    {

        //public IScopedFhirStoreBuilder<T> WithSearch()
        //{
        //    extensions.Add(new SqlScopedSearchFhirExtension<T>(
        //        new IndexService(new FhirModel(), new FhirPropertyIndex(new FhirModel()), new ResourceVisitor(new FhirPropertyIndex(new FhirModel())), new ElementIndexer(new FhirModel()),
        //        new SqlScopedIndexStore()),
        //        new SqlScopedFhirIndex<T>(), new Localhost(endpoint), new SqlScopedSnapshotStore<T>()));
        //    return this;
        //}

        //public IScopedFhirStoreBuilder<T> WithHistory()
        //{
        //    extensions.Add(new SqlScopedHistoryFhirExtension<T>());
        //    return this;
        //}

        public IScopedFhirStore<T> BuildStore(Uri baseUri, T scope)
        {
            SqlScopedFhirStore<T> store = new SqlScopedFhirStore<T>(new FormatId());
            store.Scope = scope;
            store.AddExtension(new SqlScopedSearchFhirExtension<T>(
                new IndexService(new FhirModel(), new FhirPropertyIndex(new FhirModel()),
                    new ResourceVisitor(new FhirPropertyIndex(new FhirModel())), new ElementIndexer(new FhirModel()),
                    new SqlScopedIndexStore()),
                new SqlScopedFhirIndex<T>(new FormatId()), new Localhost(baseUri), new SqlScopedSnapshotStore<T>()));
            store.AddExtension(new SqlScopedHistoryFhirExtension<T>());
            ((IBaseFhirStore)store).AddExtension(new PagingExtension(new SqlScopedSnapshotStore<T>(), new Transfer(new SqlScopedGenerator<T>(new FormatId()), new Localhost(baseUri)), new Localhost(baseUri)));

            return store;
        }

        public IScopedGenerator<T> GetGenerator(T scope)
        {
            SqlScopedGenerator<T> generator = new SqlScopedGenerator<T>(new FormatId());
            generator.Scope = scope;
            return generator;
        }
    }
}