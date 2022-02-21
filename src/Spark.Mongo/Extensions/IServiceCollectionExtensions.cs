using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spark.Core;
using Spark.Engine;
using Spark.Engine.Interfaces;
using Spark.Engine.Store.Interfaces;
using Spark.Mongo.Search.Common;
using Spark.Mongo.Search.Indexer;
using Spark.Mongo.Store;
using Spark.Mongo.Store.Extensions;
using Spark.Search.Mongo;
using Spark.Store.Mongo;

namespace Spark.Mongo.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static void AddMongoFhirStore(this IServiceCollection services, StoreSettings settings)
        {
            services.TryAddSingleton(settings);
            services.TryAddTransient<IGenerator>((provider) => new MongoIdGenerator(settings.ConnectionString));
            services.TryAddTransient<IFhirStore, MongoFhirStore>();
            services.TryAddTransient<IAsyncFhirStore>((provider) => new MongoAsyncFhirStore(settings.ConnectionString));
            services.TryAddTransient<IFhirStorePagedReader>((provider) => new MongoFhirStorePagedReader(settings.ConnectionString));
            services.TryAddTransient(provider => new HistoryStore(settings.ConnectionString));
            services.TryAddTransient<IHistoryStore>(s => s.GetRequiredService<HistoryStore>());
            services.TryAddTransient<IAsyncHistoryStore>(s => s.GetRequiredService<HistoryStore>());
            services.TryAddTransient((provider) => new MongoSnapshotStore(settings.ConnectionString));
            services.TryAddTransient<ISnapshotStore>(s => s.GetRequiredService<MongoSnapshotStore>());
            services.TryAddTransient<IAsyncSnapshotStore>(s => s.GetRequiredService<MongoSnapshotStore>());
            services.TryAddTransient<IFhirStoreAdministration>((provider) => new MongoStoreAdministration(settings.ConnectionString));
            services.TryAddTransient<MongoIndexMapper>();
            services.TryAddTransient((provider) => new MongoIndexStore(settings.ConnectionString, provider.GetRequiredService<MongoIndexMapper>()));
            services.TryAddTransient<IIndexStore>(s => s.GetRequiredService<MongoIndexStore>());
            services.TryAddTransient<IAsyncIndexStore>(s => s.GetRequiredService<MongoIndexStore>()) ;
            services.TryAddTransient((provider) => new MongoIndexStore(settings.ConnectionString, provider.GetRequiredService<MongoIndexMapper>()));
            services.TryAddTransient((provider) => DefinitionsFactory.Generate(ModelInfo.SearchParameters));
            services.TryAddTransient<MongoSearcher>();
            services.TryAddTransient<MongoFhirIndex>();
            services.TryAddTransient<IFhirIndex>(s => s.GetRequiredService<MongoFhirIndex>());
            services.TryAddTransient<IAsyncFhirIndex>(s => s.GetRequiredService<MongoFhirIndex>());
        }
    }
}
