/*
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spark.Engine;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;
using Spark.Store.MongoDB.Search.Common;
using Spark.Store.MongoDB.Search.Indexer;
using Spark.Store.MongoDB.Search;

namespace Spark.Store.MongoDB.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AddMongoFhirStore(this IServiceCollection services, StoreSettings settings)
    {
        services.TryAddSingleton(settings);
        services.TryAddTransient<IIdentityGenerator>(_ => new GuidIdentityGenerator(settings.ConnectionString));
        services.TryAddTransient<IFhirStore>(_ => new MongoFhirStore(settings.ConnectionString));
        services.TryAddTransient<IFhirStorePagedReader>(_ => new MongoFhirStorePagedReader(settings.ConnectionString));
        services.TryAddTransient<IHistoryStore>(_ => new HistoryStore(settings.ConnectionString));
        services.TryAddTransient<ISnapshotStore>(_ => new MongoSnapshotStore(settings.ConnectionString));
        services.TryAddTransient<ISnapshotStore2>(_ => new MongoSnapshotStore(settings.ConnectionString));
        services.TryAddTransient<IFhirStoreAdministration>(_ => new MongoStoreAdministration(settings.ConnectionString));
        services.TryAddTransient<MongoIndexMapper>();
        services.TryAddTransient<IIndexStore>(provider => new MongoIndexStore(settings.ConnectionString, provider.GetRequiredService<MongoIndexMapper>()));
        services.TryAddTransient(provider => new MongoIndexStore(settings.ConnectionString, provider.GetRequiredService<MongoIndexMapper>()));
        services.TryAddTransient(provider => DefinitionsFactory.Generate(provider.GetRequiredService<IFhirModel>()));
        services.TryAddTransient<MongoSearcher>();
        services.TryAddTransient<IFhirIndex, MongoFhirIndex>();
        services.TryAddSingleton(settings.IndexQueue);
        services.TryAddTransient<IIndexQueue>(provider => new MongoIndexQueue(settings.ConnectionString, provider.GetRequiredService<IndexQueueSettings>()));
    }
}
