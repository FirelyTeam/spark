using System;
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
using Spark.Engine.Service;
using Unity.WebApi;
using Spark.Mongo.Search.Indexer;
using Spark.Import;

namespace Spark
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
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
            container.RegisterType<MongoFhirStore, MongoFhirStore>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(Settings.MongoUrl));
            container.RegisterType<IFhirStore, MongoFhirStore>(new ContainerControlledLifetimeManager());
            container.RegisterType<IGenerator, MongoFhirStore>(new ContainerControlledLifetimeManager());
            container.RegisterType<ISnapshotStore, MongoFhirStore>(new ContainerControlledLifetimeManager());
            container.RegisterType<IIndexStore, MongoIndexStore>(new ContainerControlledLifetimeManager());
            container.RegisterType<MongoIndexStore>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(Settings.MongoUrl, container.Resolve<MongoIndexMapper>()));
            container.RegisterInstance<Definitions>(DefinitionsFactory.Generate(ModelInfo.SearchParameters));
            //TODO: Use FhirModel instead of ModelInfo
            container.RegisterType<IFhirIndex, MongoFhirIndex>(new ContainerControlledLifetimeManager());
            container.RegisterType<IFhirResponseFactory, FhirResponseFactory>();
            container.RegisterType<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
            container.RegisterType<IFhirResponseInterceptor, ConditionalHeaderFhirResponseInterceptor>("ConditionalHeaderFhirResponseInterceptor");
            container.RegisterType<IFhirModel, FhirModel>(new ContainerControlledLifetimeManager(), new InjectionConstructor());
            container.RegisterType<FhirPropertyIndex>(new ContainerControlledLifetimeManager(), new InjectionConstructor(container.Resolve<IFhirModel>()));

            container.RegisterType<InitializeHub>(new HierarchicalLifetimeManager());

            // register all your components with the container here
            // it is NOT necessary to register your controllers

            // e.g. container.RegisterType<ITestService, TestService>();
            IControllerFactory unityControllerFactory = new UnityControllerFactory(container);
            ControllerBuilder.Current.SetControllerFactory(unityControllerFactory);

            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container); 
            GlobalHost.DependencyResolver = new SignalRUnityDependencyResolver(container);
        }

        
    }
}