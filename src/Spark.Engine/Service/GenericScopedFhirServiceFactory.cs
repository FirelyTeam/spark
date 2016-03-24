using System;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service
{
    public class GenericScopedFhirServiceFactory : IScopedFhirServiceFactory
    {
        private readonly IScopedFhirStoreBuilder builder;

        public GenericScopedFhirServiceFactory(IScopedFhirStoreBuilder builder)
        {
            this.builder = builder;
        }


        public IScopedFhirService<T> GetFhirService<T, TKey>(Uri baseUri, Func<T, TKey> scopeKeyProvider)
        {
            return null;
        }
        public IScopedFhirService<T> GetFhirService<T>(Uri baseUri, Func<T, int> scopeKeyProvider) 
        {
            IScopedFhirStore<T> scopedFhirStore = builder.BuildStore(baseUri, scopeKeyProvider);
            return new ScopedFhirService<T>(scopedFhirStore, new FhirResponseFactory.FhirResponseFactory(new Localhost(baseUri), new FhirResponseInterceptorRunner(new []{new ConditionalHeaderFhirResponseInterceptor()})), 
                new Transfer(scopedFhirStore, new Localhost(baseUri)));
        }
    }

    public interface IScopedFhirStoreBuilder
    {
        IScopedFhirStore<T> BuildStore<T>(Uri baseUri, Func<T, int> scopeKeyProvider);
    }

    public interface IScopedFhirServiceFactory
    {
        IScopedFhirService<T> GetFhirService<T>(Uri baseUri, Func<T, int> scopeKeyProvider);
    }
  
}