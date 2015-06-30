using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Mongo.Search.Common
{
    public static class MongoDocExtention
    {
        public static void Write(this BsonDocument document, string field, BsonValue value)
        {
            if (field.StartsWith("_")) field = "PREFIX" + field;

            bool forcearray = (value != null) ? (value.BsonType == BsonType.Document) : false;
            // anders kan er op zo'n document geen $elemMatch gedaan worden.

            BsonElement element;

            if (document.TryGetElement(field, out element))
            {
                if (element.Value.BsonType == BsonType.Array)
                {
                    element.Value.AsBsonArray.Add(value);
                }
                else
                {
                    document.Remove(field);
                    document.Add(field, new BsonArray() { element.Value, value });
                }
            }
            else
            {
                if (forcearray)
                    document.Add(field, new BsonArray() { value });
                else
                    document.Add(field, value);
            }
        }
    }
}
