using System;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Spark.Engine.Service;
using Spark.Service;
using Spark.Store.Sql.Model;

namespace Spark.Store.Sql.Contracts
{

    public class SqlScopedFhirServiceFactory
    {
        public SqlScopedFhirService<T> GetFhirService<T>(IFhirDbContext context, Uri baseUri, Func<T, int> scopeKeyProvider)
        {
            SqlScopedFhirStoreBuilder<T> builder = new SqlScopedFhirStoreBuilder<T>(context, scopeKeyProvider);
            SqlScopedFhirStore<T> scopedFhirStore = builder.BuildSqlStore(baseUri);
            return new SqlScopedFhirService<T>(scopedFhirStore,
                new Engine.FhirResponseFactory.FhirResponseFactory(new Localhost(baseUri),
                    new FhirResponseInterceptorRunner(new[] {new ConditionalHeaderFhirResponseInterceptor()})),
                new Transfer(scopedFhirStore, new Localhost(baseUri)));
        }
    }

}