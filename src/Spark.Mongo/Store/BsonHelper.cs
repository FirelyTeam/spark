/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using MongoDB.Bson;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Utility;
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
        AddMetaData(bsonDocument, entry);
        return bsonDocument;
    }

    public static Resource ParseResource(BsonDocument document)
    {
        RemoveMetadata(document);
        string json = document.ToJson();
        FhirJsonDeserializer parser = new(DeserializerSettingsFactory.GetOstrichDeserializerSettings());
        return parser.Deserialize<Resource>(json);
    }

    public static Entry ExtractMetadata(BsonDocument document)
    {
        DateTime when = GetVersionDate(document);
        IKey key = GetKey(document);
        Bundle.HTTPVerb method = (Bundle.HTTPVerb)(int)document[Field.METHOD];

        RemoveMetadata(document);
        return  Entry.Create(method, key, when);
    }

    public static Entry ToEntry(this BsonDocument document, bool subsetted = false)
    {
        if (document == null) return null;

        try
        {
            Entry entry = ExtractMetadata(document);
            if (entry.IsPresent)
            {
                entry.Resource = ParseResource(document);

                if (subsetted)
                {
                    if (entry.Resource.Meta == null)
                    {
                        entry.Resource.Meta = new Meta();
                    }

                    entry.Resource.Meta.Tag.Add(new Coding
                    {
                        System = "http://terminology.hl7.org/CodeSystem/v3-ObservationValue",
                        Code = "SUBSETTED",
                        Display = "subsetted",
                    });
                }
            }
            return entry;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Mongo document contains invalid BSON to parse.", e);
        }
    }

    public static IEnumerable<Entry> ToEntries(this IEnumerable<BsonDocument> cursor, bool subsetted = false)
    {
        foreach (BsonDocument document in cursor)
        {
            Entry entry = SparkBsonHelper.ToEntry(document, subsetted);
            yield return entry;
        }
    }

    public static DateTime GetVersionDate(BsonDocument document)
    {
        BsonValue value = document[Field.WHEN];
        return value.ToUniversalTime();
    }

    public static void RemoveMetadata(BsonDocument document)
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

    public static void AddMetaData(BsonDocument document, Entry entry)
    {
        document[Field.METHOD] = entry.Method;
        document[Field.PRIMARYKEY] = entry.Key.ToOperationPath();
        document[Field.REFERENCE] = entry.Key.ToBsonReferenceKey();
        AddMetaData(document, entry.Key, entry.Resource);
    }

    private static void AssertKeyIsValid(IKey key)
    {
        bool valid = (key.Base == null) && (key.TypeName != null) && (key.ResourceId != null) && (key.VersionId != null);
        if (!valid)
        {
            throw new Exception("This key is not valid for storage: " + key.ToString());
        }
    }

    public static void AddMetaData(BsonDocument document, IKey key, Resource resource)
    {
        AssertKeyIsValid(key);
        document[Field.TYPENAME] = key.TypeName;
        document[Field.RESOURCEID] = key.ResourceId;
        document[Field.VERSIONID] = key.VersionId;

        document[Field.WHEN] = (resource!= null && resource.Meta!= null && resource.Meta.LastUpdated.HasValue)?
            resource.Meta.LastUpdated.Value.UtcDateTime : DateTime.UtcNow;
        document[Field.STATE] = Value.CURRENT;
    }

    public static IKey GetKey(BsonDocument document)
    {
        Key key = new Key
        {
            TypeName = (string)document[Field.TYPENAME],
            ResourceId = (string)document[Field.RESOURCEID],
            VersionId = (string)document[Field.VERSIONID]
        };

        return key;
    }

    public static void TransferMetadata(BsonDocument from, BsonDocument to)
    {
        to[Field.TYPENAME] = from[Field.TYPENAME];
        to[Field.RESOURCEID] = from[Field.RESOURCEID];
        to[Field.VERSIONID] = from[Field.VERSIONID];
        to[Field.WHEN] = from[Field.WHEN];
        to[Field.METHOD] = from[Field.METHOD];
        to[Field.STATE] = from[Field.STATE];
    }
}
