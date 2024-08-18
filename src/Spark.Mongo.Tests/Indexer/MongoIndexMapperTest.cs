/*
 * Copyright (c) 2015-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Search;
using Spark.Mongo.Search.Indexer;
using Spark.Engine.Model;
using Xunit;

namespace Spark.Mongo.Tests.Indexer
{
    /// <summary>
    /// Summary description for MongoIndexMapperTest
    /// </summary>
    public class MongoIndexMapperTest
    {
        private readonly MongoIndexMapper _sut;
        public MongoIndexMapperTest()
        {
            _sut = new MongoIndexMapper();
        }

        [Fact]
        public void TestMapRootIndexValue()
        {
            //"root" element should be skipped.
            IndexValue iv = new IndexValue("root");
            iv.Values.Add(new IndexValue("internal_resource", new StringValue("Patient")));

            var results = _sut.MapEntry(iv);
            Assert.Single(results);
            var result = results[0];
            Assert.True(result.IsBsonDocument);
            Assert.Equal(2, result.AsBsonDocument.ElementCount);
            var firstElement = result.AsBsonDocument.GetElement(0);
            Assert.Equal("internal_level", firstElement.Name);
            var secondElement = result.GetElement(1);
            Assert.Equal("internal_resource", secondElement.Name);
            Assert.True(secondElement.Value.IsString);
            Assert.Equal("Patient", secondElement.Value.AsString);
        }
    }
}
