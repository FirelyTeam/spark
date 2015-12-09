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
        [ExpectedException(typeof(NotImplementedException))]
        public void TestMapUnknownExpression()
        {
            Expression exp = new TokenValue("test", true);
            sut.Map(exp);
        }

        [TestMethod]
        public void TestMapIndexValue()
        {
            Expression exp = new IndexValue("name", new StringValue("test"));
            var result = sut.Map(exp);
            Assert.AreEqual("{ \"name\" : \"test\" }", result.ToJson());
        }

        [TestMethod]
        public void TestMapCompositeValue()
        {
            Expression exp = new CompositeValue(new ValueExpression[] { new IndexValue("system", new StringValue("testSystem")), new IndexValue("code", new StringValue("testCode")) });
            var result = sut.Map(exp);
            Assert.AreEqual("{ \"system\" : \"testSystem\", \"code\" : \"testCode\" }", result.ToJson());
        }

        [TestMethod]
        public void TestMapRootIndexValue()
        {
            //"root" element should be skipped.
            IndexValue iv = new IndexValue("root");
            iv.Values.Add(new IndexValue("internal_resource", new StringValue("Patient")));

            var result = sut.Map(iv);
            Assert.IsTrue(result.IsBsonDocument);
            Assert.AreEqual(1, result.AsBsonDocument.ElementCount);
            var firstElement = result.AsBsonDocument.GetElement(0);
            Assert.AreEqual("internal_resource", firstElement.Name);
            Assert.IsTrue(firstElement.Value.IsString);
            Assert.AreEqual("Patient", firstElement.Value.AsString);
        }
    }
}
