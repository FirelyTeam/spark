/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using MongoDB.Bson;
using Spark.Engine.Core;

namespace Spark.Mongo.Extensions;

internal static class EntryExtensions
{
    internal static BsonDocument ToBsonDocument(this Entry entry)
    {
        var bsonDocument = entry.Resource.ToBsonDocument();
        bsonDocument.AddMetaData(entry);
        return bsonDocument;
    }    
}
