/*
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;

namespace Spark.Mongo.Tests.Search;

internal class BsonSerializationProvider : IBsonSerializationProvider
{
    private IDictionary<Type, Func<IBsonSerializer>> _registeredBsonSerializers = new Dictionary<Type, Func<IBsonSerializer>>
    {
        { typeof(BsonNull), () => new BsonNullSerializer() },
        { typeof(string), () => new StringBsonSerializer() },
        { typeof(BsonDocument), () => new BsonDocumentSerializer() },
        { typeof(BsonDateTime), () => new BsonDateTimeSerializer() },
    };

    public IBsonSerializer GetSerializer(System.Type type)
    {
        if(_registeredBsonSerializers.ContainsKey(type))
        {
            return _registeredBsonSerializers[type].Invoke();
        }

        return null;
    }
}