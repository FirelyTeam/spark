using Hl7.Fhir.Model;
using Microsoft.AspNet.SignalR;
using Microsoft.Practices.Unity;
using Spark.Controllers;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Spark.Engine.Model;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using Spark.Filters;
using Spark.Import;
using Spark.Mock.Search.Infrastructure;
using Spark.Service;
using Spark.Store.Mock;
using System.Web.Http;
using System.Web.Mvc;
using Unity.WebApi;

namespace Spark
{
	public static class UnityConfig
    {
        public static void RegisterComponents(HttpConfiguration config)
        {
            var container = GetUnityContainer();

            // e.g. container.RegisterType<ITestService, TestService>();
            IControllerFactory unityControllerFactory = new UnityControllerFactory(container);
            ControllerBuilder.Current.SetControllerFactory(unityControllerFactory);
            
            config.DependencyResolver = new UnityDependencyResolver(container);            
            GlobalHost.DependencyResolver = new SignalRUnityDependencyResolver(container);
        }

        public static UnityContainer GetUnityContainer()
        {
            var container = new UnityContainer();

#if DEBUG
            container.AddNewExtension<UnityLogExtension>();
#endif
            container.RegisterType<HomeController, HomeController>(new PerResolveLifetimeManager());
            container.RegisterType<IServiceListener, ServiceListener>(new ContainerControlledLifetimeManager());
            container.RegisterType<ILocalhost, Localhost>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(Settings.Endpoint));
            //container.RegisterType<MongoFhirStore, MongoFhirStore>(new ContainerControlledLifetimeManager(),
            //    new InjectionConstructor(Settings.MongoUrl));
            container.RegisterType<IFhirStore, MockFhirStore>(new ContainerControlledLifetimeManager());
            container.RegisterType<IGenerator, MockGenerator>(new ContainerControlledLifetimeManager());
            container.RegisterType<ISnapshotStore, MockSnapshotStore>(new ContainerControlledLifetimeManager());
            container.RegisterType<IFhirStoreAdministration, MockStoreAdministration>(new ContainerControlledLifetimeManager());
            container.RegisterType<IIndexStore, MockIndexStore>(new ContainerControlledLifetimeManager());
        
            container.RegisterType<ITransfer, Transfer>(new ContainerControlledLifetimeManager());
            container.RegisterType<MockIndexStore>(new ContainerControlledLifetimeManager());
           // container.RegisterInstance<Definitions>(DefinitionsFactory.Generate(ModelInfo.SearchParameters));
            //TODO: Use FhirModel instead of ModelInfo
            container.RegisterType<IFhirIndex, MockFhirIndex>(new ContainerControlledLifetimeManager());
            container.RegisterType<IFhirResponseFactoryOld, FhirResponseFactoryOld>();
            container.RegisterType<IFhirResponseFactory, FhirResponseFactory>();
            container.RegisterType<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
            container.RegisterType<IFhirResponseInterceptor, ConditionalHeaderFhirResponseInterceptor>("ConditionalHeaderFhirResponseInterceptor");
            container.RegisterType<IFhirModel, FhirModel>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(SparkModelInfo.SparkSearchParameters));
            container.RegisterType<FhirPropertyIndex>(new ContainerControlledLifetimeManager(), 
                new InjectionConstructor(container.Resolve<IFhirModel>()));

            container.RegisterType<CompressionHandler>(new ContainerControlledLifetimeManager(), 
                new InjectionConstructor(Settings.MaximumDecompressedBodySizeInBytes));

            container.RegisterType<InitializerHub>(new HierarchicalLifetimeManager());
            container.RegisterType<IHistoryStore, MockHistoryStore>();
            container.RegisterType<IFhirService, FhirService>(new ContainerControlledLifetimeManager());
                  //new InjectionFactory(unityContainer => unityContainer.Resolve<IFhirService>
                  //(new DependencyOverride(typeof(IFhirServiceExtension[]), 
                  //unityContainer.Resolve<IFhirExtensionsBuilder>().GetExtensions()))));

            container.RegisterType<IServiceListener, SearchService>("searchListener");
            container.RegisterType<IFhirServiceExtension, SearchService>("search");
            container.RegisterType<ISearchService, SearchService>();
            container.RegisterType<IFhirServiceExtension, TransactionService>("transaction");
            container.RegisterType<IFhirServiceExtension, HistoryService>("history");
            container.RegisterType<IFhirServiceExtension, PagingService>("paging");
            container.RegisterType<ISnapshotPaginationProvider, SnapshotPaginationProvider>();
            container.RegisterType<ISnapshotPaginationCalculator, SnapshotPaginationCalculator>();
            container.RegisterType<IFhirServiceExtension, ResourceStorageService>("storage");
            container.RegisterType<IFhirServiceExtension, ConformanceService>("conformance");
            container.RegisterType<ICompositeServiceListener, ServiceListener>(new ContainerControlledLifetimeManager());

            // register all your components with the container here
            // it is NOT necessary to register your controllers

            return container;
        }
    }
}