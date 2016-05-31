using System;
using System.Linq;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service.ServiceIntegration
{
    public class FhirServiceFactory
    {
        public static IFhirService GetFhirService(Uri baseUri, IStorageBuilder storageBuilder,
            IServiceListener[] listeners = null)
        {
            FhirExtensionsBuilder extensionsBuilder = new FhirExtensionsBuilder(storageBuilder, baseUri);
            IGenerator generator = storageBuilder.GetStore<IGenerator>();

            return  new FhirService(extensionsBuilder.GetExtensions().ToArray(), GetFhirResponseFactory(baseUri),
                new Transfer(generator, new Localhost(baseUri)));
        }


        private static IFhirResponseFactory GetFhirResponseFactory(Uri baseUri)
        {
           return new FhirResponseFactory.FhirResponseFactory(new Localhost(baseUri),
                new FhirResponseInterceptorRunner(new[] {new ConditionalHeaderFhirResponseInterceptor()}));
        }
    }
}