using System;
using Spark.Engine.Store.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service.ServiceIntegration
{
    public class ScopedFhirServiceBuilder<TScope> : IScopedFhirServiceBuilder<TScope>
    {
        private readonly Uri baseUri;
        private readonly IStorageBuilder<TScope> storageBuilder;
        private readonly IServiceListener[] listeners;

        public ScopedFhirServiceBuilder(Uri baseUri, IStorageBuilder<TScope> storageBuilder,
            IServiceListener[] listeners = null)
        {
            this.baseUri = baseUri;
            this.storageBuilder = storageBuilder;
            this.listeners = listeners;
        }

   
        public IFhirService WithScope(TScope scope)
        {
            ScopedStorageBuilderAdapter<TScope> storageBuilderAdapter = new ScopedStorageBuilderAdapter<TScope>(storageBuilder, scope);
            return FhirServiceFactory.GetFhirService(baseUri, storageBuilderAdapter, listeners);
        }
    }
}