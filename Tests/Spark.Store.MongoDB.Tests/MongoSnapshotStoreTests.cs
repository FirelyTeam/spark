/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using Testcontainers.MongoDb;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Store.MongoDB.Tests;

[Trait("Category", "Integration")]
public class MongoSnapshotStoreTests
{
    [Fact]
    public async Task AddSnapshotAsync_WithSmallSnapshot_StoresSingleLegacyDocument()
    {
        await using var container = await StartMongoOrSkipAsync();
        var connectionString = BuildConnectionString(container.GetConnectionString(), "snapshottest");
        var store = new MongoSnapshotStore(connectionString);
        var snapshot = CreateSnapshot(totalCount: 10);

        await store.AddSnapshotAsync(snapshot);

        var loaded = await store.GetSnapshotAsync(snapshot.Id);
        var collection = GetSnapshotCollection(connectionString);

        Assert.Equal(snapshot.Id, loaded.Id);
        Assert.Null(loaded.GroupId);
        Assert.Equal(10, loaded.Keys.Count);
        Assert.Equal(1, await collection.CountDocumentsAsync(_ => true, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddSnapshotAsync_WithLargeSnapshot_StoresChunkDocumentsWithSnapshotGroupId()
    {
        await using var container = await StartMongoOrSkipAsync();
        var connectionString = BuildConnectionString(container.GetConnectionString(), "snapshottest");
        var store = new MongoSnapshotStore(connectionString);
        var snapshot = CreateSnapshot(totalCount: MongoSnapshotStore.SNAPSHOT_KEY_LIMIT + 1);

        await store.AddSnapshotAsync(snapshot);

        var collection = GetSnapshotCollection(connectionString);
        // NOTE: originalDocument will be null. When the snapshot exceeds SNAPSHOT_KEY_LIMIT Snapshot.Id is moved to
        //       Snapshot.GroupId during the Snapshot.Split and a new Key is generated for Snapshot.Id.
        var originalDocument = await collection.Find(s => s.Id == snapshot.Id).FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        var chunkCount = await collection.CountDocumentsAsync(s => s.GroupId == snapshot.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(originalDocument);
        Assert.Equal(2, chunkCount);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithOffsetInsideSingleChunk_ReturnsWindowForThatChunk()
    {
        await using var container = await StartMongoOrSkipAsync();
        var connectionString = BuildConnectionString(container.GetConnectionString(), "snapshottest");
        var store = new MongoSnapshotStore(connectionString);
        var snapshot = CreateSnapshot(totalCount: MongoSnapshotStore.SNAPSHOT_KEY_LIMIT + 100, countParam: 100);

        await store.AddSnapshotAsync(snapshot);

        var loaded = await ((ISnapshotStore2)store).GetSnapshotAsync(snapshot.Id, 100);
        var keys = new SnapshotPaginationCalculator().GetKeysForPage(loaded, 100).ToList();

        Assert.Equal(snapshot.Id, loaded.Id);
        Assert.Equal(0, loaded.StartIndex);
        Assert.Equal(MongoSnapshotStore.SNAPSHOT_KEY_LIMIT, loaded.Keys.Count);
        Assert.Equal("Patient/101/_history/1", keys.First().ToString());
        Assert.Equal("Patient/200/_history/1", keys.Last().ToString());
    }

    [Fact]
    public async Task GetSnapshotAsync_WithPageCrossingChunkBoundary_ReturnsCombinedChunkWindow()
    {
        await using var container = await StartMongoOrSkipAsync();
        var connectionString = BuildConnectionString(container.GetConnectionString(), "snapshottest");
        var store = new MongoSnapshotStore(connectionString);
        var snapshot = CreateSnapshot(totalCount: MongoSnapshotStore.SNAPSHOT_KEY_LIMIT + 100, countParam: 100);

        await store.AddSnapshotAsync(snapshot);

        var loaded = await ((ISnapshotStore2)store).GetSnapshotAsync(snapshot.Id, 980);
        var keys = new SnapshotPaginationCalculator().GetKeysForPage(loaded, 980).ToList();

        Assert.Equal(snapshot.Id, loaded.Id);
        Assert.Equal(0, loaded.StartIndex);
        Assert.Equal(MongoSnapshotStore.SNAPSHOT_KEY_LIMIT + 100, loaded.Keys.Count);
        Assert.Equal("Patient/981/_history/1", keys.First().ToString());
        Assert.Equal("Patient/1080/_history/1", keys.Last().ToString());
    }

    [Fact]
    public async Task GetSnapshotAsync_WithCustomCountParam_LoadsEnoughChunksForCustomPage()
    {
        await using var container = await StartMongoOrSkipAsync();
        var connectionString = BuildConnectionString(container.GetConnectionString(), "snapshottest");
        var store = new MongoSnapshotStore(connectionString);
        var snapshot = CreateSnapshot(totalCount: MongoSnapshotStore.SNAPSHOT_KEY_LIMIT + 100, countParam: 50);

        await store.AddSnapshotAsync(snapshot);

        var loaded = await ((ISnapshotStore2)store).GetSnapshotAsync(snapshot.Id, 990);
        var keys = new SnapshotPaginationCalculator().GetKeysForPage(loaded, 990).ToList();

        Assert.Equal(50, keys.Count);
        Assert.Equal("Patient/991/_history/1", keys.First().ToString());
        Assert.Equal("Patient/1040/_history/1", keys.Last().ToString());
    }

    [Fact]
    public async Task GetSnapshotAsync_WithLegacySingleDocument_StillFindsSnapshotById()
    {
        await using var container = await StartMongoOrSkipAsync();
        var connectionString = BuildConnectionString(container.GetConnectionString(), "snapshottest");
        var store = new MongoSnapshotStore(connectionString);
        var snapshot = CreateSnapshot(totalCount: 10, countParam: 5);

        await store.AddSnapshotAsync(snapshot);

        var loaded = await ((ISnapshotStore2)store).GetSnapshotAsync(snapshot.Id, 5);

        Assert.Equal(snapshot.Id, loaded.Id);
        Assert.Equal(10, loaded.Keys.Count);
        Assert.Null(loaded.GroupId);
    }

    private static Snapshot CreateSnapshot(int totalCount, int? countParam = null)
    {
        var keys = Enumerable.Range(1, totalCount).Select(i => $"Patient/{i}/_history/1").ToList();
        return Snapshot.Create(
            Bundle.BundleType.Searchset,
            new Uri("http://localhost/fhir/Patient"),
            keys,
            sortBy: null,
            count: countParam,
            includes: [],
            reverseIncludes: [],
            elements: null);
    }

    private static async Task<MongoDbContainer> StartMongoOrSkipAsync()
    {
        MongoDbContainer container = null;
        try
        {
            container = new MongoDbBuilder("mongo:8.2.7").Build();
            await container.StartAsync(TestContext.Current.CancellationToken);
            return container;
        }
        catch (Exception ex)
        {
            if (container != null)
                await container.DisposeAsync();
            Assert.Skip($"Docker/Testcontainers not available: {ex.Message}");
            return null;
        }
    }

    private static IMongoCollection<Snapshot> GetSnapshotCollection(string connectionString)
    {
        return MongoDatabaseFactory.GetMongoDatabase(connectionString).GetCollection<Snapshot>(Collection.SNAPSHOT);
    }

    private static string BuildConnectionString(string raw, string databaseName)
    {
        var builder = new MongoUrlBuilder(raw) { DatabaseName = $"{databaseName}-{Guid.NewGuid():N}" };
        if (!string.IsNullOrEmpty(builder.Username) && string.IsNullOrEmpty(builder.AuthenticationSource))
        {
            builder.AuthenticationSource = "admin";
        }
        return builder.ToMongoUrl().ToString();
    }
}
