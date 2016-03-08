using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service
{
    internal interface IScopedFhirSeviceBuilder
    {
        IFhirService GetFhirService(object scope);
    }

    internal class ScopedFhirSeviceBuilder<T, TS> : IScopedFhirSeviceBuilder
        where T : class 
        where TS : class, IScopedFhirStore<T>, new()
    {
        private readonly IBaseFhirResponseFactory responseFactory;
        private readonly ITransfer transfer;

        public ScopedFhirSeviceBuilder(IBaseFhirResponseFactory responseFactory, ITransfer transfer)
        {
            this.responseFactory = responseFactory;
            this.transfer = transfer;
        }
        public IFhirService GetFhirService(T scope)
        {
            TS scopedStore = new TS {Scope = scope};
            return new BaseFhirService(scopedStore, responseFactory, transfer);
        }

        public IFhirService GetFhirService(object scope)
        {
            T t = scope as T;
            if (t != null)
            {
                return GetFhirService(t);
            }
            return null;
        }
    }
}