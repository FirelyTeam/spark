using System;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Scope;
using Spark.Engine.Store.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service
{
    public class FhirServiceFactory
    {
        private readonly Func<Uri, IFhirStore> _fhirStore;

        public FhirServiceFactory(Func<Uri, IFhirStore> fhirStore)
        {
            _fhirStore = fhirStore;
        }

        public FhirServiceFactory(IFhirStoreBuilder fhirStoreBuilder)
        {
            _fhirStore = fhirStoreBuilder.BuildStore;
        }

        public IFhirService GetFhirService(Uri baseUri)
        {
            IFhirStore fhirStore = _fhirStore(baseUri);
            return new FhirService(fhirStore, new FhirResponseFactory.FhirResponseFactory(new Localhost(baseUri), new FhirResponseInterceptorRunner(new[] { new ConditionalHeaderFhirResponseInterceptor() })),
                new Transfer(fhirStore, new Localhost(baseUri)));
        }
    }

    public class GenericScopedFhirServiceFactory<T> 
    {
        private readonly Func<Uri, IScopedFhirStore<T>> _fhirStore;

        public GenericScopedFhirServiceFactory(Func<Uri, IScopedFhirStore<T>> fhirStore)
        {
            _fhirStore = fhirStore;
        }
        public GenericScopedFhirServiceFactory(IFhirStoreScopeBuilder<T> fhirStoreBuilder)
        {
            _fhirStore = fhirStoreBuilder.BuildStore;
        }

        public IScopedFhirService<T> GetFhirService(Uri baseUri)
        {
            IScopedFhirStore<T> scopedFhirStore = _fhirStore(baseUri);
            return new ScopedFhirService<T>(scopedFhirStore, new FhirResponseFactory.FhirResponseFactory(new Localhost(baseUri), new FhirResponseInterceptorRunner(new[] { new ConditionalHeaderFhirResponseInterceptor() })),
                new Transfer(scopedFhirStore, new Localhost(baseUri)));
        }
    }

    public interface IFhirStoreBuilder
    {
        IFhirStore BuildStore(Uri baseUri);
    }

    public interface IFhirStoreScopeBuilder<T>
    {
        IScopedFhirStore<T> BuildStore(Uri baseUri);
    }
 
    //public interface IFhirServiceFactory
    //{
    //    IScopedFhirService<T> GetFhirService<T>(Uri baseUri);
    //    IFhirService GetFhirService(Uri baseUri);
    //}

}