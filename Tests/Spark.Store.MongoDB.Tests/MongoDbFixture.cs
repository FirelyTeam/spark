/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Spark.Store.MongoDB.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class MongoDbFixture : IAsyncLifetime
{
    private const string MongoDbImage = "mongo:8.3.8";

    private MongoDbContainer _container;

    public async ValueTask InitializeAsync()
    {
        try
        {
            _container = new MongoDbBuilder(MongoDbImage)
                // Work around https://jira.mongodb.org/browse/SERVER-121912 on affected Linux kernels.
                .WithEnvironment("GLIBC_TUNABLES", "glibc.pthread.rseq=1")
                .Build();
            await _container.StartAsync(TestContext.Current.CancellationToken);
        }
        catch (Exception exception)
        {
            if (_container != null)
            {
                await _container.DisposeAsync();
            }

            Assert.Skip($"Docker/Testcontainers not available: {exception.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    public string CreateConnectionString(string databaseName)
    {
        return BuildConnectionString(_container.GetConnectionString(), databaseName);
    }

    public string CreateUnavailableConnectionString(string databaseName, int timeoutSeconds)
    {
        MongoUrlBuilder builder = new(_container.GetConnectionString())
        {
            DatabaseName = $"{databaseName}-{Guid.NewGuid():N}",
            Server = new MongoServerAddress("127.0.0.1", 1),
            ServerSelectionTimeout = TimeSpan.FromSeconds(timeoutSeconds),
            ConnectTimeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        SetAuthenticationSource(builder);
        return builder.ToMongoUrl().ToString();
    }

    private static string BuildConnectionString(string rawConnectionString, string databaseName)
    {
        MongoUrlBuilder builder = new(rawConnectionString)
        {
            DatabaseName = $"{databaseName}-{Guid.NewGuid():N}"
        };

        SetAuthenticationSource(builder);
        return builder.ToMongoUrl().ToString();
    }

    private static void SetAuthenticationSource(MongoUrlBuilder builder)
    {
        if (!string.IsNullOrEmpty(builder.Username) && string.IsNullOrEmpty(builder.AuthenticationSource))
        {
            builder.AuthenticationSource = "admin";
        }
    }
}
