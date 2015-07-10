/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Introspection;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Search.Mongo;

namespace Spark.Mongo.Search.Indexer
{
    public class BsonIndexDocument : BsonDocument
    {
        public string RootId;

        public BsonIndexDocument(string id)
        {
            this.RootId = id;
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