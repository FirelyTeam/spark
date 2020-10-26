using Hl7.Fhir.Model;
using Microsoft.Practices.Unity;
using Spark.Controllers;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Mongo.Search.Common;
using Spark.Service;
using Spark.Store.Mongo;
using System.Web.Http;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;
using Spark.Engine;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Unity.WebApi;
using Spark.Mongo.Search.Indexer;
using Spark.Import;
using Spark.Engine.Model;
using Spark.Engine.Search;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using Spark.Filters;
using Spark.Mongo.Store;
using Spark.Mongo.Store.Extensions;
using Spark.Search.Mongo;

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
                   new InjectionConstructor(Settings.MongoUrl));
            container.RegisterType<IGenerator, MongoIdGenerator>(new ContainerControlledLifetimeManager(), 
                new InjectionConstructor(Settings.MongoUrl));
            container.RegisterType<ISnapshotStore, MongoSnapshotStore>(new ContainerControlledLifetimeManager(),
                  new InjectionConstructor(Settings.MongoUrl));
            container.RegisterType<IFhirStoreAdministration, MongoStoreAdministration>(new ContainerControlledLifetimeManager(),
                     new InjectionConstructor(Settings.MongoUrl));
            container.RegisterType<IIndexStore, MongoIndexStore>(new ContainerControlledLifetimeManager());
            container.RegisterType<IIndexService, IndexService>(new ContainerControlledLifetimeManager());

            container.RegisterType<ITransfer, Transfer>(new ContainerControlledLifetimeManager());
            container.RegisterType<MongoIndexStore>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(Settings.MongoUrl, container.Resolve<MongoIndexMapper>()));
            container.RegisterInstance<Definitions>(DefinitionsFactory.Generate(ModelInfo.SearchParameters));
            //TODO: Use FhirModel instead of ModelInfo
            container.RegisterType<IReferenceNormalizationService, ReferenceNormalizationService>();
            container.RegisterType<IFhirModel, FhirModel>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(SparkModelInfo.SparkSearchParameters));
            container.RegisterType<IFhirIndex, MongoFhirIndex>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    container.Resolve<IIndexStore>(),
                    container.Resolve<MongoSearcher>(),
                    new SparkSettings
                    {
                        Search = new SearchSettings
                        {
                            CheckReferences = Settings.SearchCheckReferences,
                            CheckReferencesFor = Settings.SearchCheckReferencesFor
                        }
                    }));
            container.RegisterType<IFhirStorePagedReader, MongoFhirStorePagedReader>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(Settings.MongoUrl));
            container.RegisterType<IFhirResponseFactory, FhirResponseFactory>();
            container.RegisterType<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
            container.RegisterType<IFhirResponseInterceptor, ConditionalHeaderFhirResponseInterceptor>("ConditionalHeaderFhirResponseInterceptor");
            container.RegisterType<FhirPropertyIndex>(new ContainerControlledLifetimeManager(), 
                new InjectionConstructor(container.Resolve<IFhirModel>()));

            container.RegisterType<ElementIndexer>(new ContainerControlledLifetimeManager());
            container.RegisterType<ResourceVisitor>(new ContainerControlledLifetimeManager());

            container.RegisterType<CompressionHandler>(new ContainerControlledLifetimeManager(), 
                new InjectionConstructor(Settings.MaximumDecompressedBodySizeInBytes));

            container.RegisterType<InitializerHub>(new HierarchicalLifetimeManager());
            container.RegisterType<IHistoryStore, HistoryStore>(new InjectionConstructor(Settings.MongoUrl));
            container.RegisterType<IFhirService, FhirService>(new ContainerControlledLifetimeManager());
            container.RegisterType<IIndexRebuildService, IndexRebuildService>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    container.Resolve<IIndexStore>(),
                    container.Resolve<IIndexService>(),
                    container.Resolve<IFhirStorePagedReader>(),
                    new SparkSettings
                    {
                        IndexSettings = new IndexSettings
                        {
                            ClearIndexOnRebuild = Settings.ClearIndexOnRebuild,
                            ReindexBatchSize = Settings.ReindexBatchSize
                        }
                    }));

            container.RegisterType<IServiceListener, SearchService>("searchListener");
            container.RegisterType<IFhirServiceExtension, SearchService>("search");
            container.RegisterType<ISearchService, SearchService>();
            container.RegisterType<IFhirServiceExtension, TransactionService>("transaction");
            container.RegisterType<IFhirServiceExtension, HistoryService>("history");
            container.RegisterType<IFhirServiceExtension, PagingService>("paging");
            container.RegisterType<ISnapshotPaginationProvider, SnapshotPaginationProvider>();
            container.RegisterType<ISnapshotPaginationCalculator, SnapshotPaginationCalculator>();
            container.RegisterType<IFhirServiceExtension, ResourceStorageService>("storage");
            container.RegisterType<IFhirServiceExtension, CapabilityStatementService>("capabilityStatement");
            container.RegisterType<ICompositeServiceListener, ServiceListener>(new ContainerControlledLifetimeManager());

            // register all your components with the container here
            // it is NOT necessary to register your controllers

            return container;
        }
    }
}