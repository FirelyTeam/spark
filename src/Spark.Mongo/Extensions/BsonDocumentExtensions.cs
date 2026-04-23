/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using MongoDB.Bson;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Utility;
using Spark.Store.Mongo;

namespace Spark.Mongo.Extensions;

internal static class BsonDocumentExtensions
{
    internal static Resource ToResource(this BsonDocument document)
    {
        RemoveMetadata(document);
        string json = document.ToJson();
        FhirJsonDeserializer parser = new(DeserializerSettingsFactory.GetOstrichDeserializerSettings());
        return parser.Deserialize<Resource>(json);
    }
    
    internal static Entry ExtractMetadata(this BsonDocument document)
    {
        DateTime when = document.GetVersionDate();
        IKey key = document.GetKey();
        Bundle.HTTPVerb method = (Bundle.HTTPVerb)(int)document[Field.METHOD];

        document.RemoveMetadata();
        return  Entry.Create(method, key, when);
    }

    internal static Entry ToEntry(this BsonDocument document, bool subsetted = false)
    {
        if (document == null) return null;

        try
        {
            var entry = document.ExtractMetadata();
            if (!entry.IsPresent)
                return entry;
            
            entry.Resource = document.ToResource();
            if (!subsetted)
                return entry;
            
            entry.Resource.Meta ??= new Meta();

            entry.Resource.Meta.Tag.Add(new Coding
            {
                System = "http://terminology.hl7.org/CodeSystem/v3-ObservationValue",
                Code = "SUBSETTED",
                Display = "subsetted",
            });

            return entry;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Mongo document contains invalid BSON to parse.", e);
        }
    }

    internal static void AddMetaData(this BsonDocument document, Entry entry)
    {
        document[Field.METHOD] = entry.Method;
        document[Field.PRIMARYKEY] = entry.Key.ToOperationPath();
        document[Field.REFERENCE] = entry.Key.ToBsonReferenceKey();
        document.AddMetaData(entry.Key, entry.Resource);
    }

    internal static void AddMetaData(this BsonDocument document, IKey key, Resource resource)
    {
        key.AssertKeyIsValid();
        document[Field.TYPENAME] = key.TypeName;
        document[Field.RESOURCEID] = key.ResourceId;
        document[Field.VERSIONID] = key.VersionId;

        document[Field.WHEN] = (resource!= null && resource.Meta!= null && resource.Meta.LastUpdated.HasValue)?
            resource.Meta.LastUpdated.Value.UtcDateTime : DateTime.UtcNow;
        document[Field.STATE] = Value.CURRENT;
    }

    internal static void RemoveMetadata(this BsonDocument document)
    {
        document.Remove(Field.PRIMARYKEY);
        document.Remove(Field.REFERENCE);
        document.Remove(Field.WHEN);
        document.Remove(Field.STATE);
        document.Remove(Field.VERSIONID);
        document.Remove(Field.TYPENAME);
        document.Remove(Field.METHOD);
        document.Remove(Field.TRANSACTION);
    }

    internal static DateTime GetVersionDate(this BsonDocument document)
    {
        BsonValue value = document[Field.WHEN];
        return value.ToUniversalTime();
    }

    internal static IKey GetKey(this BsonDocument document)
    {
        return new Key
        {
            TypeName = (string)document[Field.TYPENAME],
            ResourceId = (string)document[Field.RESOURCEID],
            VersionId = (string)document[Field.VERSIONID]
        };
    }
}
