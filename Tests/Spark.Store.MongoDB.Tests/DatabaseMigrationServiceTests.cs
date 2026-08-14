/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Store;
using System;
using System.Linq;
using System.Threading.Tasks;
using Testcontainers.MongoDb;
using Xunit;

namespace Spark.Store.MongoDB.Tests;

[Trait("Category", "Integration")]
public class DatabaseMigrationServiceTests : IAsyncLifetime
{
    private MongoDbContainer _container;

    public async ValueTask InitializeAsync() => _container = await StartMongoOrSkipAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    [Fact]
    public async Task RefreshAsync_WithNoPersistedMigrations_UsesVersionZero()
    {
        var service = CreateService();

        await service.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, service.CurrentVersion);
        Assert.False(service.IsApplied(1));
    }

    [Fact]
    public async Task RecordCompletedAsync_PersistsMigrationAndRefreshesAnotherService()
    {
        string connectionString = CreateConnectionString();
        var service = new DatabaseMigrationService(connectionString);
        DateTime startedAt = DateTime.UtcNow.AddSeconds(-1);

        await service.RecordCompletedAsync(Migration(1, "first"), TestContext.Current.CancellationToken);

        BsonDocument document = await GetCollection(connectionString)
            .Find(Builders<BsonDocument>.Filter.Empty)
            .SingleAsync(TestContext.Current.CancellationToken);
        var refreshedService = new DatabaseMigrationService(connectionString);
        await refreshedService.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, document[Field.PRIMARYKEY].AsInt32);
        Assert.Equal("first", document["name"].AsString);
        Assert.True(document["completedAt"].IsBsonDateTime);
        Assert.InRange(document["completedAt"].ToUniversalTime(), startedAt, DateTime.UtcNow.AddSeconds(1));
        Assert.Equal(1, refreshedService.CurrentVersion);
        Assert.True(refreshedService.IsApplied(1));
    }

    [Fact]
    public async Task RecordCompletedAsync_RepeatingSameMigration_IsIdempotent()
    {
        string connectionString = CreateConnectionString();
        var firstService = new DatabaseMigrationService(connectionString);
        await firstService.RecordCompletedAsync(Migration(1, "first"), TestContext.Current.CancellationToken);
        BsonDateTime completedAt = (await GetCollection(connectionString)
            .Find(Builders<BsonDocument>.Filter.Empty)
            .SingleAsync(TestContext.Current.CancellationToken))["completedAt"].AsBsonDateTime;

        var secondService = new DatabaseMigrationService(connectionString);
        await secondService.RecordCompletedAsync(Migration(1, "first"), TestContext.Current.CancellationToken);

        BsonDocument persisted = await GetCollection(connectionString)
            .Find(Builders<BsonDocument>.Filter.Empty)
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(completedAt, persisted["completedAt"].AsBsonDateTime);
        Assert.Equal(1, secondService.CurrentVersion);
        Assert.Equal(1, await GetCollection(connectionString)
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RecordCompletedAsync_WithConflictingName_RejectsMigration()
    {
        string connectionString = CreateConnectionString();
        var firstService = new DatabaseMigrationService(connectionString);
        await firstService.RecordCompletedAsync(Migration(1, "first"), TestContext.Current.CancellationToken);
        var secondService = new DatabaseMigrationService(connectionString);

        await Assert.ThrowsAsync<DatabaseMigrationException>(() =>
            secondService.RecordCompletedAsync(Migration(1, "conflict"), TestContext.Current.CancellationToken));

        Assert.Equal(0, secondService.CurrentVersion);
    }

    [Fact]
    public async Task RecordCompletedAsync_WithSkippedVersion_RejectsMigration()
    {
        string connectionString = CreateConnectionString();
        var service = new DatabaseMigrationService(connectionString);

        await Assert.ThrowsAsync<DatabaseMigrationException>(() =>
            service.RecordCompletedAsync(Migration(2, "second"), TestContext.Current.CancellationToken));

        Assert.Equal(0, service.CurrentVersion);
        Assert.Equal(0, await GetCollection(connectionString)
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RefreshAsync_WithVersionGap_RejectsStateAndKeepsPreviousCache()
    {
        string connectionString = CreateConnectionString();
        var service = new DatabaseMigrationService(connectionString);
        await service.RecordCompletedAsync(Migration(1, "first"), TestContext.Current.CancellationToken);
        await GetCollection(connectionString).InsertOneAsync(
            PersistedMigration(3, "third"),
            cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DatabaseMigrationException>(() =>
            service.RefreshAsync(TestContext.Current.CancellationToken));

        Assert.Equal(1, service.CurrentVersion);
        Assert.True(service.IsApplied(1));
        Assert.False(service.IsApplied(2));
    }

    [Fact]
    public async Task RefreshAsync_WithInvalidPersistedMigration_RejectsState()
    {
        string connectionString = CreateConnectionString();
        await GetCollection(connectionString).InsertOneAsync(
            new BsonDocument
            {
                [Field.PRIMARYKEY] = 1,
                ["name"] = "first"
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var service = new DatabaseMigrationService(connectionString);

        await Assert.ThrowsAsync<DatabaseMigrationException>(() =>
            service.RefreshAsync(TestContext.Current.CancellationToken));

        Assert.Equal(0, service.CurrentVersion);
    }

    [Fact]
    public async Task RecordCompletedAsync_WhenPersistenceFails_DoesNotAdvanceCache()
    {
        await using MongoDbContainer container = await StartMongoOrSkipAsync();
        string connectionString = BuildConnectionString(container.GetConnectionString(), "migration-failure", 1);
        var service = new DatabaseMigrationService(connectionString);
        await service.RefreshAsync(TestContext.Current.CancellationToken);
        await container.StopAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<TimeoutException>(() =>
            service.RecordCompletedAsync(Migration(1, "first"), TestContext.Current.CancellationToken));

        Assert.Equal(0, service.CurrentVersion);
        Assert.False(service.IsApplied(1));
    }

    [Fact]
    public async Task RecordCompletedAsync_ConcurrentlyRecordingSameMigration_IsIdempotent()
    {
        string connectionString = CreateConnectionString();
        DatabaseMigrationService[] services = Enumerable.Range(0, 8)
            .Select(_ => new DatabaseMigrationService(connectionString))
            .ToArray();

        await Task.WhenAll(services.Select(service =>
            service.RecordCompletedAsync(Migration(1, "first"), TestContext.Current.CancellationToken)));

        Assert.All(services, service => Assert.Equal(1, service.CurrentVersion));
        Assert.Equal(1, await GetCollection(connectionString)
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    private DatabaseMigrationService CreateService() => new(CreateConnectionString());

    private string CreateConnectionString() =>
        BuildConnectionString(_container.GetConnectionString(), "migration");

    private static DatabaseMigration Migration(int version, string name) => new()
    {
        Version = version,
        Name = name
    };

    private static BsonDocument PersistedMigration(int version, string name) => new()
    {
        [Field.PRIMARYKEY] = version,
        ["name"] = name,
        ["completedAt"] = DateTime.UtcNow
    };

    private static IMongoCollection<BsonDocument> GetCollection(string connectionString) =>
        MongoDatabaseFactory.GetMongoDatabase(connectionString)
            .GetCollection<BsonDocument>(Collection.SchemaMigrations);

    private static async Task<MongoDbContainer> StartMongoOrSkipAsync()
    {
        MongoDbContainer container = null;
        try
        {
            container = new MongoDbBuilder("mongo:8.2.7").Build();
            await container.StartAsync(TestContext.Current.CancellationToken);
            return container;
        }
        catch (Exception exception)
        {
            if (container != null)
            {
                await container.DisposeAsync();
            }

            Assert.Skip($"Docker/Testcontainers not available: {exception.Message}");
            return null;
        }
    }

    private static string BuildConnectionString(
        string rawConnectionString,
        string databaseName,
        int? serverSelectionTimeoutSeconds = null)
    {
        var builder = new MongoUrlBuilder(rawConnectionString)
        {
            DatabaseName = $"{databaseName}-{Guid.NewGuid():N}"
        };
        if (!string.IsNullOrEmpty(builder.Username) && string.IsNullOrEmpty(builder.AuthenticationSource))
        {
            builder.AuthenticationSource = "admin";
        }

        if (serverSelectionTimeoutSeconds.HasValue)
        {
            builder.ServerSelectionTimeout = TimeSpan.FromSeconds(serverSelectionTimeoutSeconds.Value);
            builder.ConnectTimeout = TimeSpan.FromSeconds(serverSelectionTimeoutSeconds.Value);
        }

        return builder.ToMongoUrl().ToString();
    }
}
