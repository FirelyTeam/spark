using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Search;
using Spark.Mongo.Search.Indexer;
using Spark.Engine.Model;
using MongoDB.Bson;
using System.Diagnostics;

namespace Spark.Mongo.Tests.Indexer
{
    /// <summary>
    /// Summary description for MongoIndexMapperTest
    /// </summary>
    [TestClass]
    public class MongoIndexMapperTest
    {
        private MongoIndexMapper sut;
        public MongoIndexMapperTest()
        {
            sut = new MongoIndexMapper();
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
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

        [TestMethod]
        public void TestMapRootIndexValue()
        {
            //"root" element should be skipped.
            IndexValue iv = new IndexValue("root");
            iv.Values.Add(new IndexValue("internal_resource", new StringValue("Patient")));

            var results = sut.MapEntry(iv);
            Assert.AreEqual(1, results.Count);
            var result = results[0];
            Assert.IsTrue(result.IsBsonDocument);
            Assert.AreEqual(2, result.AsBsonDocument.ElementCount);
            var firstElement = result.AsBsonDocument.GetElement(0);
            Assert.AreEqual("internal_level", firstElement.Name);
            var secondElement = result.GetElement(1);
            Assert.AreEqual("internal_resource", secondElement.Name);
            Assert.IsTrue(secondElement.Value.IsString);
            Assert.AreEqual("Patient", secondElement.Value.AsString);
        }
    }
}
