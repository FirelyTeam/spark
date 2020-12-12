/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */


using MongoDB.Bson;

namespace Spark.Mongo.Search.Indexer
{
    public static class BsonDocumentExtensions
    {
        public static void Append(this BsonDocument document, string name, BsonValue value)
        {
            document.Add(name, value ?? BsonNull.Value);
        }
    
        public static void Write(this BsonDocument document, string field, BsonValue value)
        {

            if (value == null) return;

            if (field.StartsWith("_")) field = "PREFIX" + field;
            // todo: make sure the search query builder also picks up this name change.

            bool forcearray = (value.BsonType == BsonType.Document);
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
                    document.Append(field, new BsonArray() { element.Value, value ?? BsonNull.Value });
                }
            }
            else
            {
                if (forcearray)
                    document.Append(field, new BsonArray() { value ?? BsonNull.Value });
                else
                    document.Append(field, value);
            }
        }

    }
   
}