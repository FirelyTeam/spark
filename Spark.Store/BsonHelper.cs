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
        public static BsonDocument EntryToBson(Entry entry)
        {
            // todo: HACK!
            Hack.MongoPeriod(entry);

            string json = FhirSerializer.SerializeResourceToJson(entry.Resource);
            // todo: DSTU2 - this does not work anymore for deletes!!!

            BsonDocument document = BsonDocument.Parse(json);
            AddMetaData(document, entry.Resource);
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
            BsonValue value = document[Field.VERSIONDATE];
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
            document.Remove(Field.VERSIONDATE);
            document.Remove(Field.STATE);
            document.Remove(Field.VERSIONID);
            document.Remove(Field.TYPENAME);
            document.Remove(Field.OPERATION);
            document.Remove(Field.TRANSACTION);
        }

        public static void AddMetaData(BsonDocument document, Entry entry)
        {
            document[Field.OPERATION] = entry.Presense.ToString();

            if (entry.IsResource())
            {
                AddMetaData(document, entry.Resource);
            }
        }

        public static void AddMetaData(BsonDocument document, Resource resource)
        {
            document[Field.VERSIONID] = resource.Meta.VersionId;
            document[Field.TYPENAME] = resource.TypeName;
            document[Field.VERSIONDATE] = GetVersionDate(resource) ?? DateTime.UtcNow;
        }

        public static void TransferMetadata(BsonDocument from, BsonDocument to)
        {
            to[Field.STATE] = from[Field.STATE];

            to[Field.VERSIONID] = from[Field.VERSIONID];
            to[Field.VERSIONDATE] = from[Field.VERSIONDATE];
            to[Field.OPERATION] = from[Field.OPERATION];
            to[Field.TYPENAME] = from[Field.TYPENAME];
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
