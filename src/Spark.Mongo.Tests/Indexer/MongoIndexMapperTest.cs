using System;
using System.Text;
using System.Collections.Generic;
using Spark.Search;
using Spark.Mongo.Search.Indexer;
using Spark.Engine.Model;
using MongoDB.Bson;
using System.Diagnostics;
using Xunit;

namespace Spark.Mongo.Tests.Indexer
{
    /// <summary>
    /// Summary description for MongoIndexMapperTest
    /// </summary>
    public class MongoIndexMapperTest
    {
        private MongoIndexMapper sut;
        public MongoIndexMapperTest()
        {
            sut = new MongoIndexMapper();
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [Fact]
        public void TestMapRootIndexValue()
        {
            //"root" element should be skipped.
            IndexValue iv = new IndexValue("root");
            iv.Values.Add(new IndexValue("internal_resource", new StringValue("Patient")));

            var results = sut.MapEntry(iv);
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
