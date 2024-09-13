/*
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Spark.Mongo.Tests.Search;

internal class StringBsonSerializer : StringSerializer, IBsonSerializer
{

}