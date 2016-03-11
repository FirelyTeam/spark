using System;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service
{
    public class GenericScopedFhirServiceFactory<T> : IScopedFhirServiceFactory<T>
    {
        private readonly IScopedFhirStoreBuilder<T> builder;

        public GenericScopedFhirServiceFactory(IScopedFhirStoreBuilder<T> builder)
        {
            this.builder = builder;
        }

        public IFhirService GetFhirService(Uri baseUri, T scope)
        {
            IScopedFhirStore<T> scopedFhirStore = builder.BuildStore(baseUri, scope);
            IScopedGenerator<T> generator = builder.GetGenerator(scope);
            return new FhirService(scopedFhirStore, new BaseFhirResponseFactory(new Localhost(baseUri), new FhirResponseInterceptorRunner(new []{new ConditionalHeaderFhirResponseInterceptor()})), 
                new Transfer(generator, new Localhost(baseUri)));
        }
    }

    public interface IScopedFhirStoreBuilder<T>
    {
        //IScopedFhirStoreBuilder<T> WithSearch();
        //IScopedFhirStoreBuilder<T> WithHistory();
        IScopedFhirStore<T> BuildStore(Uri baseUri, T scope);
        IScopedGenerator<T> GetGenerator(T scope);
    }

    public interface IScopedFhirServiceFactory<T>
    {
        IFhirService GetFhirService(Uri baseUri, T scope);
    }
}