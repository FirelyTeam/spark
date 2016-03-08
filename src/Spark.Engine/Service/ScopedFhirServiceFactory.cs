using System;
using System.Collections.Generic;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service
{
    public class ScopedFhirServiceFactory
    {
        private readonly IBaseFhirResponseFactory responseFactory;
        private readonly ITransfer transfer;
        private readonly IDictionary<Type, IScopedFhirSeviceBuilder> builders;

        public ScopedFhirServiceFactory(IBaseFhirResponseFactory responseFactory, ITransfer transfer)
        {
            this.responseFactory = responseFactory;
            this.transfer = transfer;
            builders = new Dictionary<Type, IScopedFhirSeviceBuilder>();
        }

        public void RegisterStore<T, TS>()
            where T : class
            where TS : class, IScopedFhirStore<T>, new()
        {
            builders.Add(typeof (T), new ScopedFhirSeviceBuilder<T, TS>(responseFactory, transfer));
        }

        public IFhirService GetFhirService<T>(T scope)
        {
            return builders[typeof(T)].GetFhirService(scope);
        }
    }
}