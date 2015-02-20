using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Spark.Core;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;

namespace Spark.Store
{
    public static class SparkBsonHelper
    {
        public static BsonDocument CreateDocument(Resource resource)
        {
            if (resource != null)
            {
                // todo: HACK!
                Hack.RemoveExtensions(resource);
                string json = FhirSerializer.SerializeResourceToJson(resource);
                return BsonDocument.Parse(json);
            }
            else
            {
                return new BsonDocument();
            }
        }

        public static BsonDocument EntryToBson(Entry entry)
        {
            BsonDocument document = CreateDocument(entry.Resource);
            AddMetaData(document, entry);
            return document;
        }

        public static Entry BsonToEntry(BsonDocument document)
        {
            if (document == null) return null;

            try
            {
                DateTime stamp = GetVersionDate(document);
                RemoveMetadata(document);
                string json = document.ToJson();
                Resource resource = FhirParser.ParseResourceFromJson(json);
                
                Entry entry = new Entry(resource);
                AddVersionDate(entry, stamp);
                return entry;
            }
            catch (Exception inner)
            {
                throw new InvalidOperationException("Cannot parse MongoDb's json into a feed entry: ", inner);
            }
        }

        public static DateTime GetVersionDate(BsonDocument document)
        {
            BsonValue value = document[Field.WHEN];
            return value.ToUniversalTime();
        }

        public static void AddVersionDate(Entry entry, DateTime when)
        {
            entry.When = when;
            if (entry.Resource != null)
            {
                if (entry.Resource.Meta == null)
                    entry.Resource.Meta = new Resource.ResourceMetaComponent();

                entry.Resource.Meta.LastUpdated = when;
            }
        }

        public static void RemoveMetadata(BsonDocument document)
        {
            document.Remove(Field.PRIMARYKEY);
            document.Remove(Field.WHEN);
            document.Remove(Field.STATE);
            document.Remove(Field.VERSIONID);
            document.Remove(Field.TYPENAME);
            document.Remove(Field.PRESENSE);
            document.Remove(Field.TRANSACTION);
        }

        public static void AddMetaData(BsonDocument document, Entry entry)
        {
            document[Field.PRESENSE] = entry.Presense;
            AddMetaData(document, entry.Key);
        }

        public static void AddMetaData(BsonDocument document, Key key)
        {
            document[Field.TYPENAME] = key.TypeName;
            document[Field.RESOURCEID] = key.ResourceId;
            document[Field.VERSIONID] = key.VersionId;
            document[Field.WHEN] = DateTime.UtcNow;
            document[Field.STATE] = Value.CURRENT;
        }

        public static void TransferMetadata(BsonDocument from, BsonDocument to)
        {
            to[Field.TYPENAME] = from[Field.TYPENAME];
            to[Field.RESOURCEID] = from[Field.RESOURCEID];
            to[Field.VERSIONID] = from[Field.VERSIONID];
            to[Field.WHEN] = from[Field.WHEN];
            to[Field.PRESENSE] = from[Field.PRESENSE];
            to[Field.STATE] = from[Field.STATE];
        }

    }
}
