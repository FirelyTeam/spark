/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using MongoDB.Bson;
using Spark.Engine.Core;

namespace Spark.Store.MongoDB.Extensions;

internal static class IKeyExtensions
{
    internal static BsonValue ToBsonReferenceKey(this IKey key)
    {
        return new BsonString(key.TypeName + "/" + key.ResourceId);
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
