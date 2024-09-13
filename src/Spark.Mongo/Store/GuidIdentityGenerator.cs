/*
 * Copyright (c) 2023-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Core;
using Spark.Store.Mongo;
using System;

namespace Spark.Mongo.Store;

public class GuidIdentityGenerator : IIdentityGenerator
{
    private readonly IMongoDatabase _database;
    private readonly string _formatSpecifier;

    public GuidIdentityGenerator(string mongoUrl, string formatSpecifier = "D")
    {
        _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        _formatSpecifier = formatSpecifier;
    }
    public string NextResourceId(Resource resource)
    {
        var id = Guid.NewGuid().ToString(_formatSpecifier);
        return id;
    }

    public string NextVersionId(string resourceIdentifier) => throw new NotImplementedException();

    public string NextVersionId(string resourceType, string resourceIdentifier)
    {
        var name = resourceType + "_history_" + resourceIdentifier;
        var versionId = Next(name);
        return versionId;
    }

    private string Next(string name)
    {
        var query = Builders<BsonDocument>.Filter.Eq(Field.PRIMARYKEY, name);
        var update = Builders<BsonDocument>.Update.Inc(Field.COUNTERVALUE, 1);
        var options = new FindOneAndUpdateOptions<BsonDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After,
            Projection = Builders<BsonDocument>.Projection.Include(Field.COUNTERVALUE)
        };

        var collection = _database.GetCollection<BsonDocument>(Collection.COUNTERS);
        var document = collection.FindOneAndUpdate(query, update, options);

        return document[Field.COUNTERVALUE].AsInt32.ToString();
    }
}