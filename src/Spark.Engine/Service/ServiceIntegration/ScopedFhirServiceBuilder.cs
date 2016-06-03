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
        private readonly IFhirService fhirService;

        public ScopedFhirServiceBuilder(Uri baseUri, IStorageBuilder<TScope> storageBuilder,
            IServiceListener[] listeners = null)
        {
            this.baseUri = baseUri;
            this.storageBuilder = storageBuilder;
            this.listeners = listeners;
            this.fhirService = FhirServiceFactory.GetFhirService(baseUri, storageBuilder, listeners);
        }
   
        public IFhirService WithScope(TScope scope)
        {
            storageBuilder.ConfigureScope(scope);
            return fhirService;
        }
    }
}