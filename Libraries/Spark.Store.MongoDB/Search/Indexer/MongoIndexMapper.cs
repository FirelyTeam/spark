/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using MongoDB.Bson;
using Spark.Engine.Model;
using Spark.Engine.Search.Types;
using Spark.Store.MongoDB.Search.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Store.MongoDB.Search.Indexer;

// FIXME: [next-major-release] The whole MongoIndexMapper can be made static, there are no instance data.

/// <summary>
/// Maps IndexValue elements to BsonElements.
/// </summary>
public class MongoIndexMapper
{
    /// <summary>
    /// Meant for mapping the root IndexValue (and all the stuff below it)
    /// </summary>
    /// <param name="indexValue"></param>
    /// <returns>List of BsonDocuments, one for the root and one for each contained index in it.</returns>
    public List<BsonDocument> MapEntry(IndexValue indexValue)
    {
        if (indexValue.Name != "root")
            throw new ArgumentException("MapEntry is only meant for mapping a root IndexValue.", nameof(indexValue));

        List<BsonDocument> result = [];
        EntryToDocument(indexValue, 0, result);
        return result;
    }

    private static void EntryToDocument(IndexValue indexValue, int level, List<BsonDocument> result)
    {
        // Add the real values (not contained) to a document and add that to the result.
        List<IndexValue> indexValues =
        [
            .. indexValue.Values.Where(expression => expression is IndexValue { Name: not "contained" })
                .Select(expression => (IndexValue)expression)
        ];

        if (indexValues.Count == 0)
            return;

        BsonDocument document = new(new BsonElement(InternalField.LEVEL, level));
        document.AddRange(indexValues.Select(IndexValueToElement));
        result.Add(document);

        // Then do that recursively for all contained indexed resources.
        List<IndexValue> containedIndexValues =
        [
            .. indexValue.Values.Where(expression => expression is IndexValue { Name: "contained" })
                .Select(expression => (IndexValue)expression)
        ];
        foreach (IndexValue contained in containedIndexValues)
        {
            EntryToDocument(contained, level + 1, result);
        }
    }

    private static BsonValue Map(Expression expression)
    {
        return MapExpression((dynamic)expression);
    }

    private static BsonValue MapExpression(IndexValue indexValue)
    {
        return new BsonDocument(IndexValueToElement(indexValue));
    }

    private static BsonElement IndexValueToElement(IndexValue indexValue)
    {
        if (indexValue.Name == "_id")
            indexValue.Name = "fhir_id"; //_id is reserved in Mongo for the primary key and must be unique.

        if (indexValue.Values.Count == 1)
        {
            return new BsonElement(indexValue.Name, Map(indexValue.Values[0]));
        }
        BsonArray values = new();
        foreach (Expression value in indexValue.Values)
        {
            values.Add(Map(value));
        }
        return new BsonElement(indexValue.Name, values);
    }

    private static BsonValue MapExpression(CompositeValue composite)
    {
        BsonDocument compositeDocument = new();
        foreach (ValueExpression component in composite.Components)
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

    private static BsonValue MapExpression(StringValue stringValue)
    {
        return BsonValue.Create(stringValue.Value);
    }

    private static BsonValue MapExpression(DateTimeValue datetimeValue)
    {
        return BsonValue.Create(datetimeValue.Value.UtcDateTime);
    }

    private static BsonValue MapExpression(DateValue dateValue)
    {
        return BsonValue.Create(dateValue.Value);
    }

    private static BsonValue MapExpression(NumberValue numberValue)
    {
        return BsonValue.Create((double)numberValue.Value);
        // FIXME: MongoDB added native decimal support in version 3.4. The below comment is therefore not correct
        //        anymore, but we will keep the comment as a historic reference.
        // TODO: double is not as accurate as decimal, but MongoDB has no support for decimal.
        // https://docs.mongodb.org/v2.6/tutorial/model-monetary-data/#monetary-value-exact-precision.
    }
}
