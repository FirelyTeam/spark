/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Model;
using Spark.Engine.Search.Types;
using Spark.Store.MongoDB.Search.Indexer;
using MongoDB.Bson;
using System.Collections.Generic;
using Xunit;

namespace Spark.Store.MongoDB.Tests.Indexer;

public class MongoIndexMapperTest
{
    private readonly MongoIndexMapper _indexMapper;

    public MongoIndexMapperTest(ITestOutputHelper output)
    {
        _indexMapper = new MongoIndexMapper();
    }

    [Fact]
    public void TestMapRootIndexValue()
    {
        // "root" element should be skipped.
        IndexValue indexValue = new("root");
        indexValue.Values.Add(new IndexValue("internal_resource", new StringValue("Patient")));

        List<BsonDocument> indexedEntries = _indexMapper.MapEntry(indexValue);
        Assert.Single(indexedEntries);
        BsonDocument indexedEntry = indexedEntries[0];
        Assert.True(indexedEntry.IsBsonDocument);
        Assert.Equal(2, indexedEntry.ElementCount);
        BsonElement firstIndexedElement = indexedEntry.GetElement(0);
        Assert.Equal("internal_level", firstIndexedElement.Name);
        BsonElement secondIndexedElement = indexedEntry.GetElement(1);
        Assert.Equal("internal_resource", secondIndexedElement.Name);
        Assert.True(secondIndexedElement.Value.IsString);
        Assert.Equal("Patient", secondIndexedElement.Value.AsString);
    }
}
