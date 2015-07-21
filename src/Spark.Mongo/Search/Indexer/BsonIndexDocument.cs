/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */


using MongoDB.Bson;
using Spark.Engine.Core;

namespace Spark.Mongo.Search.Indexer
{
    public class BsonIndexDocument : BsonDocument
    {
        public string RootId;

        public BsonIndexDocument(IKey key)
        {
            this.RootId = key.TypeName + "/" + key.ResourceId;
        }

        public void Write(string field, BsonValue value)
        {
            if (field.StartsWith("_")) field = "PREFIX" + field;
            // todo: make sure the search query builder also picks up this name change.

            bool forcearray = (value != null) ? (value.BsonType == BsonType.Document) : false;
            // anders kan er op zo'n document geen $elemMatch gedaan worden.

            BsonElement element;

            if (this.TryGetElement(field, out element))
            {
                if (element.Value.BsonType == BsonType.Array)
                {
                    element.Value.AsBsonArray.Add(value);
                }
                else
                {
                    this.Remove(field);
                    this.Add(field, new BsonArray() { element.Value, value });
                }
            }
            else
            {
                if (forcearray)
                    this.Add(field, new BsonArray() { value });
                else
                    this.Add(field, value);
            }
        }

    }
   
}