/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Spark.Engine.Model;
using Spark.Engine.Search.Types;
using Spark.Store.MongoDB.Search.Common;
using Spark.Store.MongoDB.Search.Indexer;
using System;
using System.Threading.Tasks;
using Xunit;
using Task = System.Threading.Tasks.Task;
using Spark.Store.MongoDB.Tests;

namespace Spark.Store.MongoDB.Tests.Search;

[Trait("Category", "Integration")]
[Collection("MongoDB integration")]
public class MongoIndexStoreIntegrationTests
{
    private readonly MongoDbFixture _mongo;

    public MongoIndexStoreIntegrationTests(MongoDbFixture mongo) => _mongo = mongo;

    [Fact]
    public async Task SaveAsync_ThrowsDuplicateKeyWhenStaleVersionFollowsNewerVersion()
    {
        (MongoIndexStore indexStore, IMongoCollection<BsonDocument> collection) = await CreateIndexStoreAsync();
        await indexStore.SaveAsync(CreateIndexValue(version: 2));

        MongoCommandException exception = await Assert.ThrowsAsync<MongoCommandException>(
            () => indexStore.SaveAsync(CreateIndexValue(version: 1)));

        Assert.Equal(11000, exception.Code);

        BsonDocument storedDocument = await collection
            .Find(Builders<BsonDocument>.Filter.Eq(InternalField.ID, "Patient/patient-1"))
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, storedDocument[InternalField.VERSION].ToInt64());
    }

    [Fact]
    public async Task SaveAsync_SameVersionIsAllowedToBeReIndexed()
    {
        (MongoIndexStore indexStore, IMongoCollection<BsonDocument> collection) = await CreateIndexStoreAsync();
        await indexStore.SaveAsync(CreateIndexValue(version: 2));

        // Re-index the same version, this is allowed.
        await indexStore.SaveAsync(CreateIndexValue(version: 2));

        BsonDocument storedDocument = await collection
            .Find(Builders<BsonDocument>.Filter.Eq(InternalField.ID, "Patient/patient-1"))
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, storedDocument[InternalField.VERSION].ToInt64());
    }

    private async Task<(MongoIndexStore IndexStore, IMongoCollection<BsonDocument> Collection)> CreateIndexStoreAsync()
    {
        string connectionString = _mongo.CreateConnectionString("indexstore");
        IMongoDatabase database = MongoDatabaseFactory.GetMongoDatabase(connectionString);
        IMongoCollection<BsonDocument> collection =
            database.GetCollection<BsonDocument>(MongoCollections.SEARCH_INDEX_COLLECTION);

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending(InternalField.ID),
                new CreateIndexOptions { Unique = true, Sparse = true }),
            cancellationToken: TestContext.Current.CancellationToken);

        MongoIndexStore indexStore = new(
            connectionString,
            new MongoIndexMapper(),
            new NullLogger<MongoIndexStore>()
        );

        return (indexStore, collection);
    }

    private static IndexValue CreateIndexValue(long version) => new(
        "root",
        new IndexValue(InternalField.ID, new StringValue("Patient/patient-1")),
        new IndexValue(InternalField.VERSION, new NumberValue(version)));

}
