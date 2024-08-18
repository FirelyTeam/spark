/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using MongoDB.Bson;
using Spark.Engine.Model;
using Spark.Mongo.Search.Common;
using Spark.Search;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Mongo.Search.Indexer
{
    //Maps IndexValue elements to BsonElements.
    public class MongoIndexMapper
    {
        /// <summary>
        /// Meant for mapping the root IndexValue (and all the stuff below it)
        /// </summary>
        /// <param name="indexValue"></param>
        /// <returns>List of BsonDocuments, one for the root and one for each contained index in it.</returns>
        public List<BsonDocument> MapEntry(IndexValue indexValue)
        {
            var result = new List<BsonDocument>();

            if (indexValue.Name == "root")
            {
                EntryToDocument(indexValue, 0, result);
                return result;
            }
            else throw new ArgumentException("MapEntry is only meant for mapping a root IndexValue.", "indexValue");
        }

        private void EntryToDocument(IndexValue indexValue, int level, List<BsonDocument> result)
        {
            //Add the real values (not contained) to a document and add that to the result.
            List<IndexValue> notNestedValues = indexValue.Values.Where(exp => (exp is IndexValue) && ((IndexValue)exp).Name != "contained").Select(exp => (IndexValue)exp).ToList();
            var doc = new BsonDocument(new BsonElement(InternalField.LEVEL, level));
            doc.AddRange(notNestedValues.Select(iv => IndexValueToElement(iv)));
            result.Add(doc);

            //Then do that recursively for all contained indexed resources.
            List<IndexValue> containedValues = indexValue.Values.Where(exp => (exp is IndexValue) && ((IndexValue)exp).Name == "contained").Select(exp => (IndexValue)exp).ToList();
            foreach (var contained in containedValues)
            {
                EntryToDocument(contained, level + 1, result);
            }
        }

        private BsonValue Map(Expression expression)
        {
            return MapExpression((dynamic)expression);
        }

        private BsonValue MapExpression(IndexValue indexValue)
        {
            return new BsonDocument(IndexValueToElement(indexValue));
        }

        private BsonElement IndexValueToElement(IndexValue indexValue)
        {
            if (indexValue.Name == "_id")
                indexValue.Name = "fhir_id"; //_id is reserved in Mongo for the primary key and must be unique.

            if (indexValue.Values.Count == 1)
            {
                return new BsonElement(indexValue.Name, Map(indexValue.Values[0]));
            }
            BsonArray values = new BsonArray();
            foreach (var value in indexValue.Values)
            {
                values.Add(Map(value));
            }
            return new BsonElement(indexValue.Name, values);
        }

        private BsonValue MapExpression(CompositeValue composite)
        {
            BsonDocument compositeDocument = new BsonDocument();
            foreach (var component in composite.Components)
            {
                if (component is IndexValue value)
                {
                    compositeDocument.Add(IndexValueToElement(value));
                }
                else
                {
                    throw new ArgumentException("All Components of composite are expected to be of type IndexValue");
                }
            }
            return compositeDocument;
        }

        private BsonValue MapExpression(StringValue stringValue)
        {
            return BsonValue.Create(stringValue.Value);
        }

        private BsonValue MapExpression(DateTimeValue datetimeValue)
        {
            return BsonValue.Create(datetimeValue.Value.UtcDateTime);
        }

        private BsonValue MapExpression(DateValue dateValue)
        {
            return BsonValue.Create(dateValue.Value);
        }

        private BsonValue MapExpression(NumberValue numberValue)
        {
            return BsonValue.Create((double)numberValue.Value);
            //TODO: double is not as accurate as decimal, but MongoDB has no support for decimal.
            //https://docs.mongodb.org/v2.6/tutorial/model-monetary-data/#monetary-value-exact-precision.
        }
    }
}
