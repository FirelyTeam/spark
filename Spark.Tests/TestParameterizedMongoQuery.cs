using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Spark.Search;
using MongoDB.Bson;

namespace Spark.Tests
{
    [TestClass]
    public class ParameterizedMongoQueryTest
    {
        [TestMethod]
        public void TestSimpleParameter()
        {
            IMongoQuery query = Query.EQ("name", "$name");
            string name = "Teun";
            IMongoQuery query2 = new QueryDocument(BsonDocument.Parse(query.ToString().Replace("$name", name)));
            Assert.AreEqual("{ \"name\" : \"Teun\" }", query2.ToString());
        }

        [TestMethod]
        public void TestArrayParameter()
        {
            IMongoQuery query = Query.In("name", new BsonArray(){"$names"});
            var names = new BsonArray() {"Teun", "Truus"};
            IMongoQuery query2 = new QueryDocument(BsonDocument.Parse(query.ToString().Replace("$names", String.Join(",", names))));
            Assert.AreEqual("{ \"name\" : { \"$in\" : [\"Teun,Truus\"] } }", query2.ToString());
        }
    }
}

