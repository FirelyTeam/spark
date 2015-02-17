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

        public static void AddVersionDate(Entry entry, DateTime stamp)
        {
            // todo: DSTU2
            /*
            if (resource is Resource)
            {
                (resource as ResourceEntry).LastUpdated = stamp;
            }
            if (resource is DeletedEntry)
            {
                (resource as DeletedEntry).When = stamp;
            }
            */
            entry.When = stamp;
        }

        public static void RemoveMetadata(BsonDocument document)
        {
            document.Remove(Field.RECORDID);
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

        public static DateTime? GetVersionDate(Resource resource)
        {
            DateTimeOffset? result = resource.Meta.LastUpdated;
            // todo: DSTU2
            /*(resource is ResourceEntry)
            ? ((ResourceEntry)resource).LastUpdated
            : ((DeletedEntry)resource).When;
            */

            // todo: moet een ontbrekende version date niet in de service gevuld worden?
            //return (result != null) ? result.Value.UtcDateTime : null;
            return (result != null) ? result.Value.UtcDateTime : (DateTime?)null;
        }


    }
}
