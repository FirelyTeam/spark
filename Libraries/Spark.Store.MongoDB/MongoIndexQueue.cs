/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Engine.Store;
using Spark.Store.MongoDB.Extensions;

namespace Spark.Store.MongoDB;

public class MongoIndexQueue : IIndexQueue
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly IndexQueueSettings _settings;
    private readonly string _workerId;

    public MongoIndexQueue(string mongoUrl, IndexQueueSettings settings)
    {
        var database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        _collection = database.GetCollection<BsonDocument>(Collection.INDEX_QUEUE);
        _settings = settings;
        _workerId = $"{Environment.MachineName}:{Environment.ProcessId}";
        EnsureIndexes();
    }

    public async Task EnqueueAsync(Entry entry, CancellationToken cancellationToken = default)
    {
        // FIXME: This is papering over a problem how ToBsonDocument() is asserting keys - AssertKeyIsValid().
        //        ToBsonDocument() is made specifically for the resources collection, but we use it here since it works
        //        good enough for us. It is also tied in with how ToEntry() works in ClaimNextAsync(), ToEntry() was
        //        also made specifically for the 'resources' collection.
        var newEntry = Entry.Create(entry.Method,
            Key.Create(entry.Key.TypeName, entry.Key.ResourceId, entry.Key.VersionId), entry.Resource);
        var document = new BsonDocument
        {
            [Field.PRIMARYKEY] = ObjectId.GenerateNewId().ToString(),
            [IndexQueueField.ENTRY] = newEntry.ToBsonDocument(),
            [IndexQueueField.STATUS] = IndexQueueStatus.PENDING,
            [IndexQueueField.WORKER_ID] = BsonNull.Value,
            [IndexQueueField.CLAIMED_AT] = BsonNull.Value,
            [IndexQueueField.LEASE_EXPIRES_AT] = BsonNull.Value,
            [IndexQueueField.ATTEMPTS] = 0,
            [IndexQueueField.LAST_ERROR] = BsonNull.Value,
            [IndexQueueField.ENQUEUED_AT] = DateTime.UtcNow,
        };

        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IndexQueueEntry> ClaimNextAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Claim the oldest pending entry, or reclaim a processing entry whose lease has expired.
        var filter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq(IndexQueueField.STATUS, IndexQueueStatus.PENDING),
                Builders<BsonDocument>.Filter.Lt(IndexQueueField.ATTEMPTS, _settings.MaxAttempts)
            ),
            Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq(IndexQueueField.STATUS, IndexQueueStatus.PROCESSING),
                Builders<BsonDocument>.Filter.Lt(IndexQueueField.LEASE_EXPIRES_AT, now)
            )
        );

        var update = Builders<BsonDocument>.Update
            .Set(IndexQueueField.STATUS, IndexQueueStatus.PROCESSING)
            .Set(IndexQueueField.WORKER_ID, _workerId)
            .Set(IndexQueueField.CLAIMED_AT, now)
            .Set(IndexQueueField.LEASE_EXPIRES_AT, now.Add(_settings.LeaseTimeout));

        var options = new FindOneAndUpdateOptions<BsonDocument>
        {
            ReturnDocument = ReturnDocument.After,
            Sort = Builders<BsonDocument>.Sort.Ascending(IndexQueueField.ENQUEUED_AT),
        };

        var document = await _collection
            .FindOneAndUpdateAsync(filter, update, options, cancellationToken)
            .ConfigureAwait(false);

        if (document is null)
            return null;

        return new IndexQueueEntry
        {
            Id = document[Field.PRIMARYKEY].AsString,
            Entry = document[IndexQueueField.ENTRY].AsBsonDocument.ToEntry(),
            Attempts = document[IndexQueueField.ATTEMPTS].AsInt32,
            LastError = document[IndexQueueField.LAST_ERROR] == BsonNull.Value
                ? null
                : document[IndexQueueField.LAST_ERROR].AsString,
            EnqueuedAt = document[IndexQueueField.ENQUEUED_AT].ToUniversalTime(),
        };
    }

    public async Task AcknowledgeAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq(Field.PRIMARYKEY, id);
        await _collection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
    }

    public async Task NackAsync(string id, string error, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq(Field.PRIMARYKEY, id);

        // Atomically increment attempts and set status to failed or pending using an
        // aggregation pipeline update (requires MongoDB 4.2+).
        BsonDocument[] stages =
        [
            new BsonDocument("$set", new BsonDocument
            {
                [IndexQueueField.ATTEMPTS] = new BsonDocument("$add",
                    new BsonArray { $"${IndexQueueField.ATTEMPTS}", 1 }),
                [IndexQueueField.LAST_ERROR] = error,
                [IndexQueueField.WORKER_ID] = BsonNull.Value,
                [IndexQueueField.CLAIMED_AT] = BsonNull.Value,
                [IndexQueueField.LEASE_EXPIRES_AT] = BsonNull.Value,
                [IndexQueueField.STATUS] = new BsonDocument("$cond", new BsonDocument
                {
                    ["if"] = new BsonDocument("$gte", new BsonArray
                    {
                        new BsonDocument("$add", new BsonArray { $"${IndexQueueField.ATTEMPTS}", 1 }),
                        _settings.MaxAttempts,
                    }),
                    ["then"] = IndexQueueStatus.FAILED,
                    ["else"] = IndexQueueStatus.PENDING,
                }),
            }),
        ];

        PipelineDefinition<BsonDocument, BsonDocument> pipeline = stages;
        var update = Builders<BsonDocument>.Update.Pipeline(pipeline);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private void EnsureIndexes()
    {
        var indexes = new List<CreateIndexModel<BsonDocument>>
        {
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys
                    .Ascending(IndexQueueField.STATUS)
                    .Ascending(IndexQueueField.LEASE_EXPIRES_AT)),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys
                    .Ascending(IndexQueueField.STATUS)
                    .Ascending(IndexQueueField.ENQUEUED_AT)),
        };

        _collection.Indexes.CreateMany(indexes);
    }
}
