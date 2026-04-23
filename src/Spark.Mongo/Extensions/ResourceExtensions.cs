/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using MongoDB.Bson;
using Spark.Engine.Utility;

namespace Spark.Mongo.Extensions;

internal static class ResourceExtensions
{
    internal static BsonDocument ToBsonDocument(this Resource resource)
    {
        if (resource == null)
            return [];

        if (StaticReferenceToFhirModel.FhirModel == null)
            throw new InvalidOperationException($"{nameof(StaticReferenceToFhirModel)}.FhirModel is not set.");

        BaseFhirJsonSerializer serializer = new(StaticReferenceToFhirModel.FhirModel.GetModelInspector());
        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream);
        serializer.Serialize(resource, writer);
        writer.Flush();
        return BsonDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()));
    }
}
