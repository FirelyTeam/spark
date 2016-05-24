using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Spark.Engine.Scope;
using Spark.Service;

namespace Spark.Engine.Service
{
    public class ScopedFhirService<T> : FhirService, IScopedFhirService<T>
    {
        private readonly IScopedFhirStore<T> fhirStore;

        public ScopedFhirService(IScopedFhirStore<T> fhirStore, IFhirResponseFactory responseFactory, ITransfer transfer, IFhirModel fhirModel):
            base(fhirStore, responseFactory, transfer, fhirModel)
        {
            this.fhirStore = fhirStore;
        }

        public IFhirService WithScope(T scope)
        {
            fhirStore.Scope = scope;
            return this;
        }
    }
}