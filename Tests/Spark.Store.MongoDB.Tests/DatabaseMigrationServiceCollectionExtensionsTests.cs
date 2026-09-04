/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Spark.Engine;
using Spark.Engine.Core;
using Spark.Engine.Search;
using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;
using Spark.Store.MongoDB.Extensions;
using Spark.Store.MongoDB.Search;
using System;
using Xunit;

namespace Spark.Store.MongoDB.Tests;

public class DatabaseMigrationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMongoFhirStore_RegistersMigrationServiceAsSingleton()
    {
        ServiceCollection services = new();
        services.AddMongoFhirStore(CreateSettings());

        using ServiceProvider provider = services.BuildServiceProvider();

        IDatabaseMigrationService first = provider.GetRequiredService<IDatabaseMigrationService>();
        IDatabaseMigrationService second = provider.GetRequiredService<IDatabaseMigrationService>();
        Assert.Same(first, second);
        Assert.IsType<DatabaseMigrationService>(first);
    }

    [Fact]
    public void AddMongoFhirStore_WhenCalledTwice_RegistersOneMigrationRefreshService()
    {
        ServiceCollection services = new();
        StoreSettings settings = CreateSettings();

        services.AddMongoFhirStore(settings);
        services.AddMongoFhirStore(settings);

        ServiceDescriptor descriptor = Assert.Single(
            services,
            service =>
                service.ServiceType == typeof(IHostedService) &&
                service.ImplementationType == typeof(DatabaseMigrationRefreshService));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddMongoFhirStore_MongoSearcherUsesRegisteredMigrationService()
    {
        ServiceCollection services = new();
        Mock<IDatabaseMigrationService> migrationService = new();
        migrationService
            .Setup(service => service.IsApplied(DatabaseMigrations.StructuredStringTokenIndex.Version))
            .Returns(true);
        Localhost localhost = new(new Uri("http://localhost/fhir"));
        services.AddSingleton<IDatabaseMigrationService>(migrationService.Object);
        services.AddSingleton<ILocalhost>(localhost);
        services.AddSingleton(new Mock<IFhirModel>().Object);
        services.AddSingleton<IReferenceNormalizationService>(new ReferenceNormalizationService(localhost));
        services.AddLogging();
        services.AddMongoFhirStore(CreateSettings());

        using ServiceProvider provider = services.BuildServiceProvider();

        MongoSearcher searcher = provider.GetRequiredService<MongoSearcher>();
        Assert.False(searcher.IncludePlainStringTokenQuery);
    }

    private static StoreSettings CreateSettings() => new()
    {
        ConnectionString = "mongodb://localhost/spark"
    };
}
