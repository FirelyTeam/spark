/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.IO;
using System.Text;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using MongoDB.Bson;

namespace Spark.Mongo.Extensions;

internal static class ResourceExtensions
{
    internal static BsonDocument ToBsonDocument(this Resource resource)
    {
        if (resource == null)
            return [];

        BaseFhirJsonSerializer serializer = new(ModelInfo.ModelInspector);
        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream);
        serializer.Serialize(resource, writer);
        writer.Flush();
        return BsonDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()));
    }
}
