/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Store;
using Spark.Store.MongoDB.Search.Common;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Spark.Store.MongoDB.Tests;

[Trait("Category", "Integration")]
[Collection("MongoDB integration")]
public class DatabaseMigrationServiceTests
{
    private readonly MongoDbFixture _mongo;

    public DatabaseMigrationServiceTests(MongoDbFixture mongo) => _mongo = mongo;

    [Fact]
    public async Task RefreshAsync_WithFreshDatabase_RecordsCurrentMigration()
    {
        var service = CreateService();

        await service.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, service.CurrentVersion);
        Assert.True(service.IsApplied(DatabaseMigrations.StructuredStringTokenIndex.Version));
        Assert.True(service.IsApplied(DatabaseMigrations.TokenQuantityAndReferenceArrayIndex.Version));
    }

    [Fact]
    public async Task RefreshAsync_WithUnversionedResources_UsesVersionZero()
    {
        string connectionString = CreateConnectionString();
        await GetDatabase(connectionString)
            .GetCollection<BsonDocument>(Collection.RESOURCE)
            .InsertOneAsync(new BsonDocument("resource", true),
                cancellationToken: TestContext.Current.CancellationToken);
        var service = new DatabaseMigrationService(connectionString);

        await service.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, service.CurrentVersion);
        Assert.False(service.IsApplied(1));
        Assert.Equal(0, await GetCollection(connectionString)
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RefreshAsync_WithUnversionedSearchIndex_UsesVersionZero()
    {
        string connectionString = CreateConnectionString();
        await GetDatabase(connectionString)
            .GetCollection<BsonDocument>(MongoCollections.SEARCH_INDEX_COLLECTION)
            .InsertOneAsync(new BsonDocument("index", true),
                cancellationToken: TestContext.Current.CancellationToken);
        var service = new DatabaseMigrationService(connectionString);

        await service.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, service.CurrentVersion);
        Assert.False(service.IsApplied(1));
        Assert.Equal(0, await GetCollection(connectionString)
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RefreshAsync_DoesNotReclassifyLegacyDatabaseAsFresh()
    {
        string connectionString = CreateConnectionString();
        IMongoCollection<BsonDocument> resources = GetDatabase(connectionString)
            .GetCollection<BsonDocument>(Collection.RESOURCE);
        await resources.InsertOneAsync(
            new BsonDocument("resource", true),
            cancellationToken: TestContext.Current.CancellationToken);
        var service = new DatabaseMigrationService(connectionString);

        await service.RefreshAsync(TestContext.Current.CancellationToken);
        await resources.DeleteManyAsync(
            FilterDefinition<BsonDocument>.Empty,
            TestContext.Current.CancellationToken);
        await service.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, service.CurrentVersion);
        Assert.False(service.IsApplied(1));
        Assert.Equal(0, await GetCollection(connectionString)
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty,
                cancellationToken: TestContext.Current.CancellationToken));
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
        string connectionString = _mongo.CreateUnavailableConnectionString("migration-failure", 1);
        var service = new DatabaseMigrationService(connectionString);

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
        _mongo.CreateConnectionString("migration");

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
        GetDatabase(connectionString)
            .GetCollection<BsonDocument>(Collection.SchemaMigrations);

    private static IMongoDatabase GetDatabase(string connectionString) =>
        MongoDatabaseFactory.GetMongoDatabase(connectionString);

}
