using Hl7.Fhir.Model;
using Microsoft.Practices.Unity;
using Spark.Controllers;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Models;
using Spark.Mongo.Search.Common;
using Spark.Service;
using Spark.Store.Mongo;
using System.Web.Http;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Unity.WebApi;
using Spark.Mongo.Search.Indexer;
using Spark.Import;
using Spark.Engine.Model;
using Spark.Engine.Service;
using Spark.Engine.Service.Extensions;
using Spark.Engine.Storage.StoreExtensions;
using Spark.Engine.Store.Interfaces;
using Spark.Filters;
using Spark.Mongo.Store;

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
            container.RegisterType<HomeController, HomeController>(new PerResolveLifetimeManager(),
                new InjectionConstructor(Settings.MongoUrl));
            container.RegisterType<IServiceListener, ServiceListener>(new ContainerControlledLifetimeManager());
            container.RegisterType<ILocalhost, Localhost>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(Settings.Endpoint));
            //container.RegisterType<MongoFhirStore, MongoFhirStore>(new ContainerControlledLifetimeManager(),
            //    new InjectionConstructor(Settings.MongoUrl));
            container.RegisterType<IFhirStore, MongoFhirStore>(new ContainerControlledLifetimeManager(),
                   new InjectionConstructor(Settings.MongoUrl, typeof(IFhirStoreExtension[])));
            container.RegisterType<IGenerator, MongoIdGenerator>(new ContainerControlledLifetimeManager(), 
                new InjectionConstructor(Settings.MongoUrl));
            container.RegisterType<ISnapshotStore, MongoSnapshotStore>(new ContainerControlledLifetimeManager(),
                  new InjectionConstructor(Settings.MongoUrl));
            container.RegisterType<IFhirStoreAdministration, MongoFhirStoreOther>(new ContainerControlledLifetimeManager(),
                     new InjectionConstructor(Settings.MongoUrl, typeof(IFhirStore)));
            container.RegisterType<IIndexStore, MongoIndexStore>(new ContainerControlledLifetimeManager());
            container.RegisterType<IFhirService, FhirService>(new ContainerControlledLifetimeManager());
            container.RegisterType<ITransfer, Transfer>(new ContainerControlledLifetimeManager());
            container.RegisterType<MongoIndexStore>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(Settings.MongoUrl, container.Resolve<MongoIndexMapper>()));
            container.RegisterInstance<Definitions>(DefinitionsFactory.Generate(ModelInfo.SearchParameters));
            //TODO: Use FhirModel instead of ModelInfo
            container.RegisterType<IFhirIndex, MongoFhirIndex>(new ContainerControlledLifetimeManager());
            container.RegisterType<IFhirResponseFactoryOld, FhirResponseFactoryOld>();
            container.RegisterType<IFhirResponseFactory, FhirResponseFactory>();
            container.RegisterType<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
            container.RegisterType<IFhirResponseInterceptor, ConditionalHeaderFhirResponseInterceptor>("ConditionalHeaderFhirResponseInterceptor");
            container.RegisterType<IFhirModel, FhirModel>(new ContainerControlledLifetimeManager(), new InjectionConstructor(SparkModelInfo.ApiAssembly(), SparkModelInfo.SparkSearchParameters));
            container.RegisterType<FhirPropertyIndex>(new ContainerControlledLifetimeManager(), new InjectionConstructor(container.Resolve<IFhirModel>()));

            container.RegisterType<CompressionHandler>(new ContainerControlledLifetimeManager(), new InjectionConstructor(Settings.MaximumDecompressedBodySizeInBytes));

            container.RegisterType<InitializerHub>(new HierarchicalLifetimeManager());
            container.RegisterType<IFhirStoreExtension, SearchExtension>("searchExtension");
            container.RegisterType<IFhirStoreExtension, PagingExtension>("pagingExtension");
            // register all your components with the container here
            // it is NOT necessary to register your controllers

            return container;
        }
    }
}