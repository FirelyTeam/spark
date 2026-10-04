/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System.Threading.Tasks;
using Spark.Engine.Model;
using Spark.Store.MongoDB.Search.Indexer;
using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Store.MongoDB.Search.Common;

public class MongoIndexStore : IIndexStore2
{
    private IMongoDatabase _database;
    private MongoIndexMapper _indexMapper;
    private readonly ILogger<MongoIndexStore> _logger;
    public IMongoCollection<BsonDocument> Collection;

    public MongoIndexStore(string mongoUrl, MongoIndexMapper indexMapper, ILogger<MongoIndexStore> logger)
    {
        _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        _indexMapper = indexMapper;
        _logger = logger;
        Collection = _database.GetCollection<BsonDocument>(MongoCollections.SEARCH_INDEX_COLLECTION);
    }

    [Obsolete("Use ctor MongoIndexStore(string, MongoIndexMapper, ILogger<MongoIndexStore>) instead.")]
    public MongoIndexStore(string mongoUrl, MongoIndexMapper indexMapper)
    {
        _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        _indexMapper = indexMapper; 
        Collection = _database.GetCollection<BsonDocument>(MongoCollections.SEARCH_INDEX_COLLECTION);
    }

    public async Task SaveAsync(IndexValue indexValue)
    {
        var result = _indexMapper.MapEntry(indexValue);

        foreach (var doc in result)
        {
            await SaveAsync(doc).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<IndexStoreWriteFailure>> SaveBatchAsync(IReadOnlyList<IndexValue> indexValues)
    {
        List<WriteModel<BsonDocument>> operations = [];
        List<int> operationOwners = [];
        List<IndexStoreWriteFailure> failures = [];

        for (int valueIndex = 0; valueIndex < indexValues.Count; valueIndex++)
        {
            try
            {
                foreach (BsonDocument document in _indexMapper.MapEntry(indexValues[valueIndex]))
                {
                    operations.Add(CreateReplaceOneModel(document));
                    operationOwners.Add(valueIndex);
                }
            }
            catch (Exception exception)
            {
                failures.Add(new IndexStoreWriteFailure
                {
                    IndexValueIndex = valueIndex,
                    Exception = exception
                });
            }
        }

        if (operations.Count == 0)
        {
            return failures;
        }

        try
        {
            await Collection.BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = true })
                .ConfigureAwait(false);
        }
        catch (MongoBulkWriteException<BsonDocument> exception)
        {
            HashSet<int> failedValueIndexes = [];

            if (exception.WriteErrors.Count > 0 && exception.WriteConcernError == null)
            {
                int firstFailedOperation = exception.WriteErrors.Min(error => error.Index);
                int firstFailedValue = operationOwners[firstFailedOperation];
                foreach (int valueIndex in operationOwners.Where(valueIndex => valueIndex >= firstFailedValue))
                {
                    failedValueIndexes.Add(valueIndex);
                }
            }
            else
            {
                foreach (int valueIndex in operationOwners)
                {
                    failedValueIndexes.Add(valueIndex);
                }
            }

            failures.AddRange(failedValueIndexes.Select(valueIndex => new IndexStoreWriteFailure
            {
                IndexValueIndex = valueIndex,
                Exception = exception
            }));
        }
        catch (Exception exception)
        {
            failures.AddRange(operationOwners.Distinct().Select(valueIndex => new IndexStoreWriteFailure
            {
                IndexValueIndex = valueIndex,
                Exception = exception
            }));
        }

        return failures.OrderBy(failure => failure.IndexValueIndex).ToList();
    }

    private async Task SaveAsync(BsonDocument document)
    {
        string keyvalue = document.GetValue(InternalField.ID).ToString();

        if (document.TryGetValue(InternalField.VERSION, out BsonValue versionBson))
        {
            long newVersion = versionBson.ToInt64();

            var conditionalFilter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq(InternalField.ID, keyvalue),
                Builders<BsonDocument>.Filter.Or(
                    Builders<BsonDocument>.Filter.Exists(InternalField.VERSION, false),
                    Builders<BsonDocument>.Filter.Lte(InternalField.VERSION, newVersion)
                )
            );
            try
            {
                await Collection.FindOneAndReplaceAsync(conditionalFilter, document,
                    new FindOneAndReplaceOptions<BsonDocument> { IsUpsert = true }).ConfigureAwait(false);
            }
            catch (MongoCommandException ex) when (ex.Code == 11000)
            {
                _logger?.LogError(ex, "Duplicate key: a newer version is already indexed — stale write, skip.");
                throw;
            }
        }
        else
        {
            // No version info (backward compat).
            var query = Builders<BsonDocument>.Filter.Eq(InternalField.ID, keyvalue);
            await Collection.ReplaceOneAsync(query, document, new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
        }
    }

    private static ReplaceOneModel<BsonDocument> CreateReplaceOneModel(BsonDocument document)
    {
        string keyvalue = document.GetValue(InternalField.ID).ToString();
        FilterDefinition<BsonDocument> filter;

        if (document.TryGetValue(InternalField.VERSION, out BsonValue versionBson))
        {
            long newVersion = versionBson.ToInt64();
            filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq(InternalField.ID, keyvalue),
                Builders<BsonDocument>.Filter.Or(
                    Builders<BsonDocument>.Filter.Exists(InternalField.VERSION, false),
                    Builders<BsonDocument>.Filter.Lte(InternalField.VERSION, newVersion)
                )
            );
        }
        else
        {
            filter = Builders<BsonDocument>.Filter.Eq(InternalField.ID, keyvalue);
        }

        return new ReplaceOneModel<BsonDocument>(filter, document) { IsUpsert = true };
    }

    public async Task DeleteAsync(Entry entry)
    {
        string id = entry.Key.WithoutVersion().ToOperationPath();
        var query = Builders<BsonDocument>.Filter.Eq(InternalField.ID, id);
        await Collection.DeleteManyAsync(query).ConfigureAwait(false);
    }

    public async Task CleanAsync()
    {
        await Collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty).ConfigureAwait(false);
    }
}
