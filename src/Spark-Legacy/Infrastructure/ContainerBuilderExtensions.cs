using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using Hl7.Fhir.Model;
using Spark.Core;
using Spark.Engine;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Spark.Engine.Search;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using Spark.Mongo.Search.Common;
using Spark.Mongo.Search.Indexer;
using Spark.Mongo.Store;
using Spark.Mongo.Store.Extensions;
using Spark.Search.Mongo;
using Spark.Service;
using Spark.Store.Mongo;
using System.Reflection;

namespace Spark.Infrastructure
{
    public static class ContainerBuilderExtensions
    {
        public static void AddFhir(this ContainerBuilder builder, SparkSettings settings, params Assembly[] controllerAssemblies)
        {
            builder.Register(context => settings).SingleInstance();
            builder.RegisterType<ElementIndexer>();
            builder.RegisterType<ReferenceNormalizationService>().As<IReferenceNormalizationService>();
            builder.RegisterType<IndexService>().As<IIndexService>();
            builder.Register(context => new Localhost(Settings.Endpoint)).As<ILocalhost>();
            builder.Register(context => new FhirModel(ModelInfo.SearchParameters)).As<IFhirModel>();
            builder.Register(context => new FhirPropertyIndex(context.Resolve<IFhirModel>()));
            builder.RegisterType<Transfer>().As<ITransfer>();
            builder.RegisterType<ConditionalHeaderFhirResponseInterceptor>();
            builder.Register(context => new IFhirResponseInterceptor[] { context.Resolve<ConditionalHeaderFhirResponseInterceptor>() });
            builder.RegisterType<FhirResponseInterceptorRunner>().As<IFhirResponseInterceptorRunner>();
            builder.RegisterType<FhirResponseFactory>().As<IFhirResponseFactory>();
            builder.RegisterType<IndexRebuildService>().As<IIndexRebuildService>();
            builder.RegisterType<SearchService>().As<ISearchService>();
            builder.RegisterType<SnapshotPaginationProvider>().As<ISnapshotPaginationProvider>();
            builder.RegisterType<SnapshotPaginationCalculator>().As<ISnapshotPaginationCalculator>();
            builder.RegisterType<SearchService>().As<IServiceListener>();
            builder.Register(context => new[] { context.Resolve<IServiceListener>() });
            builder.RegisterType<SearchService>();
            builder.RegisterType<TransactionService>().As<ITransactionService>();
            builder.RegisterType<AsyncTransactionService>().As<IAsyncTransactionService>();
            builder.RegisterType<HistoryService>();
            builder.RegisterType<PagingService>();
            builder.RegisterType<ResourceStorageService>();
            builder.RegisterType<CapabilityStatementService>();
            builder.RegisterType<PatchService>();
            builder.RegisterType<ServiceListener>().As<ICompositeServiceListener>();

            builder.Register(context => new IFhirServiceExtension[]
            {
                context.Resolve<SearchService>(),
                context.Resolve<ITransactionService>(),
                context.Resolve<IAsyncTransactionService>(),
                context.Resolve<HistoryService>(),
                context.Resolve<PagingService>(),
                context.Resolve<ResourceStorageService>(),
                context.Resolve<CapabilityStatementService>(),
                context.Resolve<PatchService>(),
            });

            builder.RegisterType<FhirService>().As<IFhirService>().SingleInstance();
            builder.RegisterType<AsyncFhirService>().As<IAsyncFhirService>().SingleInstance();

            builder.RegisterControllers(controllerAssemblies).InstancePerRequest();
            builder.RegisterApiControllers(controllerAssemblies).InstancePerRequest();
        }

        public static void AddMongoFhirStore(this ContainerBuilder builder, StoreSettings settings)
        {
            builder.Register(context => settings).SingleInstance();
            builder.Register(context => new MongoIdGenerator(settings.ConnectionString)).As<IGenerator>().SingleInstance();
            builder.Register(context => new MongoFhirStore(settings.ConnectionString)).As<IFhirStore>();
            builder.Register(context => new MongoFhirStorePagedReader(settings.ConnectionString)).As<IFhirStorePagedReader>();
            builder.Register(context => new HistoryStore(settings.ConnectionString)).As<IHistoryStore>();
            builder.Register(context => new MongoSnapshotStore(settings.ConnectionString)).As<ISnapshotStore>();
            builder.Register(context => new MongoStoreAdministration(settings.ConnectionString)).As<IFhirStoreAdministration>();
            builder.RegisterType<MongoIndexMapper>();
            builder.Register(context => new MongoIndexStore(settings.ConnectionString, context.Resolve<MongoIndexMapper>())).As<IIndexStore>();
            builder.Register(context => new MongoIndexStore(settings.ConnectionString, context.Resolve<MongoIndexMapper>()));
            builder.Register(context => DefinitionsFactory.Generate(ModelInfo.SearchParameters));
            builder.RegisterType<MongoSearcher>();
            builder.RegisterType<MongoFhirIndex>().As<IFhirIndex>();
        }
    }
}