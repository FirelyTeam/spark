using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
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

namespace Spark.Mongo
{
    public static class IServiceCollectionExtensions
    {
        public static void AddMongoFhirStore(this IServiceCollection services, StoreSettings settings)
        {
            services.AddSingleton<StoreSettings>(settings);
            services.AddTransient<IGenerator, MongoIdGenerator>((provider) => new MongoIdGenerator(settings.ConnectionString));
            services.AddTransient<IFhirStore, MongoFhirStore>((provider) => new MongoFhirStore(settings.ConnectionString));
            services.AddTransient<IHistoryStore, HistoryStore>((provider) => new HistoryStore(settings.ConnectionString));
            services.AddTransient<ISnapshotStore, MongoSnapshotStore>((provider) => new MongoSnapshotStore(settings.ConnectionString));
            services.AddTransient<IFhirStoreAdministration, MongoStoreAdministration>((provider) => new MongoStoreAdministration(settings.ConnectionString));
            services.AddTransient<MongoIndexMapper>();
            services.AddTransient<IIndexStore, MongoIndexStore>((provider) => new MongoIndexStore(settings.ConnectionString, provider.GetRequiredService<MongoIndexMapper>()));
            services.AddTransient((provider) => new MongoIndexStore(settings.ConnectionString, provider.GetRequiredService<MongoIndexMapper>()));
            services.AddTransient((provider) => DefinitionsFactory.Generate(ModelInfo.SearchParameters));
            services.AddTransient<MongoIndexer>();
            services.AddTransient<MongoSearcher>();
            services.AddTransient<IFhirIndex, MongoFhirIndex>();
        }
    }
}
