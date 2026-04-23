/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using MongoDB.Bson;
using Spark.Engine.Core;
using Spark.Mongo.Extensions;

namespace Spark.Store.Mongo;

public static class SparkBsonHelper
{
    // FIXME: Move all extension methods into appropriate classes, i.e. ResourceExtensions, KeyExtensions, etc.

    public static BsonValue ToBsonReferenceKey(this IKey key)
    {
        return new BsonString(key.TypeName + "/" + key.ResourceId);
    }

    public static BsonDocument ToBsonDocument(this Entry entry)
    {
        var bsonDocument = entry.Resource.CreateBsonDocument();
        bsonDocument.AddMetaData(entry);
        return bsonDocument;
    }

    internal static void AssertKeyIsValid(this IKey key)
    {
        bool valid = (key.Base == null) && (key.TypeName != null) && (key.ResourceId != null) && (key.VersionId != null);
        if (!valid)
        {
            throw new Exception("This key is not valid for storage: " + key.ToString());
        }
    }
}
