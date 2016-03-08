using System;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service
{
    public class GenericScopedFhirServiceFactory<T> : IScopedFhirServiceFactory<T>
    {
        private readonly IBaseFhirResponseFactory responseFactory;
        private readonly ITransfer transfer;
        private readonly IScopedFhirStoreBuilder<T> builder;

        public GenericScopedFhirServiceFactory(IScopedFhirStoreBuilder<T> builder, IBaseFhirResponseFactory responseFactory, ITransfer transfer)
        {
            this.responseFactory = responseFactory;
            this.transfer = transfer;
            this.builder = builder;
        }


        public IFhirService GetFhirService(T scope)
        {
            IScopedFhirStore<T> scopedFhirStore = builder.BuildStore();
            scopedFhirStore.Scope = scope;
            return new BaseFhirService(scopedFhirStore, responseFactory, transfer);
        }
    }

    public interface IScopedFhirStoreBuilder<T>
    {
        IScopedFhirStore<T> BuildStore();
    }

    public interface IScopedFhirServiceFactory<T>
    {
        IFhirService GetFhirService(T scope);
    }
}