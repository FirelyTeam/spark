/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly)
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
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

            if (document.TryGetElement(field, out BsonElement element))
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
                if (value.BsonType == BsonType.Document)
                    document.Append(field, new BsonArray() { value ?? BsonNull.Value });
                else
                    document.Append(field, value);
            }
        }
    }
}