using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using Spark.Core;
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
        public static void AddMongoFhirStore(this IServiceCollection services, MongoStoreSettings settings)
        {
            services.AddTransient<IGenerator, MongoIdGenerator>((provider) => new MongoIdGenerator(settings.Url));
            services.AddTransient<IFhirStore, MongoFhirStore>((provider) => new MongoFhirStore(settings.Url));
            services.AddTransient<IHistoryStore, HistoryStore>((provider) => new HistoryStore(settings.Url));
            services.AddTransient<ISnapshotStore, MongoSnapshotStore>((provider) => new MongoSnapshotStore(settings.Url));
            services.AddTransient<IFhirStoreAdministration, MongoStoreAdministration>((provider) => new MongoStoreAdministration(settings.Url));
            services.AddTransient<MongoIndexMapper>();
            services.AddTransient<IIndexStore, MongoIndexStore>((provider) => new MongoIndexStore(settings.Url, provider.GetRequiredService<MongoIndexMapper>()));
            services.AddTransient((provider) => new MongoIndexStore(settings.Url, provider.GetRequiredService<MongoIndexMapper>()));
            services.AddTransient((provider) => DefinitionsFactory.Generate(ModelInfo.SearchParameters));
            services.AddTransient<MongoIndexer>();
            services.AddTransient<MongoSearcher>();
            services.AddTransient<IFhirIndex, MongoFhirIndex>();
        }
    }
}
