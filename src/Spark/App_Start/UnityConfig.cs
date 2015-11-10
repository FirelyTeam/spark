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
using Unity.WebApi;

namespace Spark
{
    public static class UnityConfig
    {
        public static void RegisterComponents(HttpConfiguration config)
        {
            var container = new UnityContainer();

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
            container.RegisterType<MongoIndexStore, MongoIndexStore>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(Settings.MongoUrl));
            container.RegisterInstance<Definitions>(DefinitionsFactory.Generate(ModelInfo.SearchParameters));
            //TODO: Use FhirModel instead of ModelInfo
            container.RegisterType<IFhirIndex, MongoFhirIndex>(new ContainerControlledLifetimeManager());
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