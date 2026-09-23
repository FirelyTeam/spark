/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Store.MongoDB.Tests;

[Trait("Category", "Integration")]
[Collection("MongoDB integration")]
public class MongoSnapshotStoreTests
{
    private readonly MongoDbFixture _mongo;

    public MongoSnapshotStoreTests(MongoDbFixture mongo) => _mongo = mongo;

    [Fact]
    public async Task CreateExpiryIndexAsync_CreatesIndexThatExpiresSnapshots()
    {
        var connectionString = _mongo.CreateConnectionString("snapshotexpiry");
        var snapshotStoreSettings = new SnapshotStoreSettings();

        await MongoSnapshotStore.CreateExpiryIndexAsync(
            MongoDatabaseFactory.GetMongoDatabase(connectionString),
            snapshotStoreSettings.RetentionSeconds,
            TestContext.Current.CancellationToken);

        Assert.Equal(snapshotStoreSettings.RetentionSeconds, await GetExpirySecondsAsync(connectionString));
    }

    [Fact]
    public async Task CreateExpiryIndexAsync_WithARetentionOfItsOwn_ExpiresSnapshotsAfterThat()
    {
        // StoreSettings.SnapshotStore.RetentionSeconds, which a deployment can set to something else than an hour.
        var connectionString = _mongo.CreateConnectionString("snapshotexpiryconfigured");

        await MongoSnapshotStore.CreateExpiryIndexAsync(
            MongoDatabaseFactory.GetMongoDatabase(connectionString),
            retentionSeconds: 120,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(120, await GetExpirySecondsAsync(connectionString));
    }

    [Fact]
    public async Task AddSnapshotAsync_StoresWhenCreatedAsTheDateTheExpiryIndexReads()
    {
        // The index is on WhenCreated.DateTime. 
        // This test ensures that the stored snapshot has the correct DateTime type.
        var connectionString = _mongo.CreateConnectionString("snapshotwhencreated");
        var store = new MongoSnapshotStore(connectionString);

        await store.AddSnapshotAsync(CreateSnapshot(totalCount: 1));

        var document = await MongoDatabaseFactory.GetMongoDatabase(connectionString)
            .GetCollection<BsonDocument>(Collection.SNAPSHOT)
            .Find(FilterDefinition<BsonDocument>.Empty)
            .FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(BsonType.DateTime, document["WhenCreated"]["DateTime"].BsonType);
    }

    [Fact]
    public async Task CreateExpiryIndexAsync_WhenTheIndexHasAnotherExpiry_ChangesTheExpiry()
    {
        // As after SnapshotStore.RetentionSeconds has changed: the index is already there, with the old expiry time.
        var connectionString = _mongo.CreateConnectionString("snapshotexpirychange");
        var database = MongoDatabaseFactory.GetMongoDatabase(connectionString);
        await database.GetCollection<BsonDocument>(Collection.SNAPSHOT).Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("WhenCreated.DateTime"),
                new CreateIndexOptions { ExpireAfter = TimeSpan.FromMinutes(5) }),
            cancellationToken: TestContext.Current.CancellationToken);

        var snapshotStoreSettings = new SnapshotStoreSettings();

        await MongoSnapshotStore.CreateExpiryIndexAsync(
            database, snapshotStoreSettings.RetentionSeconds, TestContext.Current.CancellationToken);

        Assert.Equal(snapshotStoreSettings.RetentionSeconds, await GetExpirySecondsAsync(connectionString));
    }

    [Fact]
    public async Task AddSnapshotAsync_WithSmallSnapshot_StoresSingleLegacyDocument()
    {
        var connectionString = _mongo.CreateConnectionString("snapshottest");
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
        var connectionString = _mongo.CreateConnectionString("snapshottest");
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
        var connectionString = _mongo.CreateConnectionString("snapshottest");
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
        var connectionString = _mongo.CreateConnectionString("snapshottest");
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
        var connectionString = _mongo.CreateConnectionString("snapshottest");
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
        var connectionString = _mongo.CreateConnectionString("snapshottest");
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

    private static IMongoCollection<Snapshot> GetSnapshotCollection(string connectionString)
    {
        return MongoDatabaseFactory.GetMongoDatabase(connectionString).GetCollection<Snapshot>(Collection.SNAPSHOT);
    }

    private static async Task<int> GetExpirySecondsAsync(string connectionString)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var indexes = await (await GetSnapshotCollection(connectionString).Indexes.ListAsync(cancellationToken))
            .ToListAsync(cancellationToken);
        var expiry = Assert.Single(indexes, index => index["key"].AsBsonDocument.Contains("WhenCreated.DateTime"));
        return expiry["expireAfterSeconds"].ToInt32();
    }

}
