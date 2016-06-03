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
            FhirExtensionsBuilder extensionsBuilder     = new FhirExtensionsBuilder(storageBuilder, baseUri);
            return GetFhirService(baseUri, extensionsBuilder, storageBuilder, listeners);
        }

        public static IFhirService GetFhirService(Uri baseUri, IFhirExtensionsBuilder extensionsBuilder,
            IStorageBuilder storageBuilder, //we won't need this anymore if we can remove the Transfer dependency for IFhirService
            IServiceListener[] listeners = null)
        {
            IGenerator generator = storageBuilder.GetStore<IGenerator>();
            IFhirServiceExtension[] extensions = extensionsBuilder.GetExtensions().ToArray();
            IServiceListener[] computedListeners = (listeners ?? Enumerable.Empty<IServiceListener>())
                                                          .Union(extensions.OfType<IServiceListener>())
                                                          .ToArray();
            ICompositeServiceListener serviceListener = new ServiceListener(new Localhost(baseUri), computedListeners);
            Transfer transfer = new Transfer(generator, new Localhost(baseUri));

            return new FhirService(extensions,
                GetFhirResponseFactory(baseUri),
                transfer,
                serviceListener);
        }


        private static IFhirResponseFactory GetFhirResponseFactory(Uri baseUri)
        {
           return new FhirResponseFactory.FhirResponseFactory(new Localhost(baseUri),
                new FhirResponseInterceptorRunner(new[] {new ConditionalHeaderFhirResponseInterceptor()}));
        }
    }
}