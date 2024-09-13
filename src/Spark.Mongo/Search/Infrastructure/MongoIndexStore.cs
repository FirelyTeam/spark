/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using System.Threading.Tasks;
using Spark.Store.Mongo;
using Spark.Engine.Model;
using Spark.Mongo.Search.Indexer;
using Spark.Engine.Store.Interfaces;

namespace Spark.Mongo.Search.Common;

public class MongoIndexStore : IIndexStore
{
    private IMongoDatabase _database;
    private MongoIndexMapper _indexMapper;
    public IMongoCollection<BsonDocument> Collection;

    public MongoIndexStore(string mongoUrl, MongoIndexMapper indexMapper)
    {
        _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        _indexMapper = indexMapper; 
        Collection = _database.GetCollection<BsonDocument>(Config.MONGOINDEXCOLLECTION);
    }

    public async Task SaveAsync(IndexValue indexValue)
    {
        var result = _indexMapper.MapEntry(indexValue);

        foreach (var doc in result)
        {
            await SaveAsync(doc).ConfigureAwait(false);
        }
    }

    public async Task SaveAsync(BsonDocument document)
    {
        string keyvalue = document.GetValue(InternalField.ID).ToString();
        var query = Builders<BsonDocument>.Filter.Eq(InternalField.ID, keyvalue);
        await Collection.ReplaceOneAsync(query, document, new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
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