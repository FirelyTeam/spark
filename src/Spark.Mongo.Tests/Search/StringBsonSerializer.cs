/* 
 * Copyright (c) 2020, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Spark.Mongo.Tests.Search
{
    internal class StringBsonSerializer : StringSerializer, IBsonSerializer
    {

    }
}
