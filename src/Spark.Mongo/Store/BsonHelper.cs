using System;
using System.Collections.Generic;
using MongoDB.Bson;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Extensions;

namespace Spark.Store.Mongo
{
    public static class SparkBsonHelper
    {
        public static BsonDocument CreateDocument(Resource resource)
        {
            if (resource != null)
            {
                string json = FhirSerializer.SerializeResourceToJson(resource);
                return BsonDocument.Parse(json);
            }
            else
            {
                return new BsonDocument();
            }
        }

        public static BsonValue ToBsonReferenceKey(this IKey key)
        {
            return new BsonString(key.TypeName + "/" + key.ResourceId);
        }

        public static BsonDocument ToBsonDocument(this Interaction entry)
        {
            BsonDocument document = CreateDocument(entry.Resource);
            AddMetaData(document, entry);
            return document;
        }

        public static Resource ParseResource(BsonDocument document)
        {
            RemoveMetadata(document);
            string json = document.ToJson();
            Resource resource = FhirParser.ParseResourceFromJson(json);
            return resource;
        }

        public static Interaction ExtractMetadata(BsonDocument document)
        {
            DateTime when = GetVersionDate(document);
            IKey key = GetKey(document);
            Bundle.HTTPVerb method = (Bundle.HTTPVerb)(int)document[Field.METHOD];

            RemoveMetadata(document);
            return  Interaction.Create(method, key, when);
        }

        public static Interaction ToInteraction(this BsonDocument document)
        {
            if (document == null) return null;

            try
            {
                Interaction entry = ExtractMetadata(document);
                if (entry.IsPresent)
                {
                    entry.Resource = ParseResource(document);
                }
                return entry;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Mongo document contains invalid BSON to parse.", e);
            }
        }

        public static IEnumerable<Interaction> ToInteractions(this MongoCursor<BsonDocument> cursor)
        {
            foreach (BsonDocument document in cursor)
            {
                Interaction entry = SparkBsonHelper.ToInteraction(document);
                yield return entry;
            }
        }

        public static DateTime GetVersionDate(BsonDocument document)
        {
            BsonValue value = document[Field.WHEN];
            return value.ToUniversalTime();
        }

        private static void ensureMeta(Resource resource)
        {
            if (resource.Meta == null)
                resource.Meta = new Meta();
        }

        public static void AddVersionDate(Interaction entry, DateTime when)
        {
            entry.When = when;
            if (entry.Resource != null)
            {
                ensureMeta(entry.Resource);
                entry.Resource.Meta.LastUpdated = when;
            }
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

        public static void AddMetaData(BsonDocument document, Interaction interaction)
        {
            document[Field.METHOD] = interaction.Method;
            document[Field.PRIMARYKEY] = interaction.Key.ToOperationPath();
            document[Field.REFERENCE] = interaction.Key.ToBsonReferenceKey();
            AddMetaData(document, interaction.Key);
        }

        private static void AssertKeyIsValid(IKey key)
        {
            bool valid = (key.Base == null) && (key.TypeName != null) && (key.ResourceId != null) && (key.VersionId != null);
            if (!valid)
            {
                throw new Exception("This key is not valid for storage: " + key.ToString());
            }
        }

        public static void AddMetaData(BsonDocument document, IKey key)
        {
            AssertKeyIsValid(key); 
            document[Field.TYPENAME] = key.TypeName;
            document[Field.RESOURCEID] = key.ResourceId;
            document[Field.VERSIONID] = key.VersionId;
            
            document[Field.WHEN] = DateTime.UtcNow;
            document[Field.STATE] = Value.CURRENT;
        }

        public static IKey GetKey(BsonDocument document)
        {
            Key key = new Key();
            key.TypeName = (string)document[Field.TYPENAME];
            key.ResourceId = (string)document[Field.RESOURCEID];
            key.VersionId = (string)document[Field.VERSIONID];

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
}
