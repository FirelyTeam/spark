/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spark.Engine;
using Spark.Engine.Store.Interfaces;
using Spark.Store.MongoDB.Extensions;
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

    private static StoreSettings CreateSettings() => new()
    {
        ConnectionString = "mongodb://localhost/spark"
    };
}
