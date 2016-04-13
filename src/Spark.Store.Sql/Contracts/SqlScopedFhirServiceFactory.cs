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
        private readonly SqlScopedFhirStoreBuilder builder;

        public SqlScopedFhirServiceFactory(IFhirDbContext context)
        {
            builder = new SqlScopedFhirStoreBuilder(context);
        }

        public IScopedFhirService<T> GetFhirService<T, TKey>(Uri baseUri, Func<T, TKey> scopeKeyProvider)
        {
            return null;
        }
        public SqlScopedFhirService<T> GetFhirService<T>(Uri baseUri, Func<T, int> scopeKeyProvider)
        {
            SqlScopedFhirStore<T> scopedFhirStore = builder.BuildSqlStore(baseUri, scopeKeyProvider);
            return new SqlScopedFhirService<T>(scopedFhirStore, new Engine.FhirResponseFactory.FhirResponseFactory(new Localhost(baseUri), new FhirResponseInterceptorRunner(new[] { new ConditionalHeaderFhirResponseInterceptor() })),
                new Transfer(scopedFhirStore, new Localhost(baseUri)));
        }
    }

    //public class SqlScopedFhirServiceFactory : GenericScopedFhirServiceFactory
    //{
    //    public SqlScopedFhirServiceFactory(IFhirDbContext context) : base(new SqlScopedFhirStoreBuilder(context))
    //    {
    //    }
    //}
}