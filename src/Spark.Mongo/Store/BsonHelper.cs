/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using MongoDB.Bson;
using Spark.Engine.Core;
using Spark.Mongo.Extensions;

namespace Spark.Store.Mongo;

public static class SparkBsonHelper
{
    // FIXME: Move all extension methods into appropriate classes, i.e. ResourceExtensions, KeyExtensions, etc.

    public static BsonDocument ToBsonDocument(this Entry entry)
    {
        var bsonDocument = entry.Resource.CreateBsonDocument();
        bsonDocument.AddMetaData(entry);
        return bsonDocument;
    }
}
