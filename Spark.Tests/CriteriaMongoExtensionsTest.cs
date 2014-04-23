using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hl7.Fhir.Model;
using Hl7.Fhir.Search;
using Spark.Search;
using M = MongoDB.Driver.Builders;
using MongoDB.Bson;
using System.Collections.Generic;
using MongoDB.Driver;

namespace Spark.Tests
{
    [TestClass]
    public class CriteriaMongoExtensionsTest
    {
        private IMongoQuery createSimpleQuery(Query query) // without chains
        {
            IMongoQuery resourceFilter = query.ResourceFilter();
            var criteria = query.Criteria.Select(c => Criterium.Parse(c));
            IMongoQuery criteriaFilter = M.Query.And(criteria.Select(c => c.ToFilter(query.ResourceType)));
            return M.Query.And(resourceFilter, criteriaFilter);
        }

        private void AssertQueriesEqual(string expected, string actual)
        {
            Assert.AreEqual(expected.Replace(" ", String.Empty), actual.Replace(" ", String.Empty));
        }

        [TestMethod]
        public void TestNumberQuery()
        {
            var query = new Query().For("ImagingStudy").AddParameter("size", "10");
            var mongoQuery = createSimpleQuery(query);

            Assert.IsNotNull(mongoQuery);
            AssertQueriesEqual("{ \"internal_level\" : 0, \"internal_resource\" : \"ImagingStudy\", \"size\" : \"10\" }", mongoQuery.ToString());
        }

        [TestMethod]
        public void TestStringQueryDefault() //default is partial from start
        {
            var query = new Query().For("Patient").AddParameter("name", "Teun");
            var mongoQuery = createSimpleQuery(query);

            Assert.IsNotNull(mongoQuery);
            AssertQueriesEqual("{ \"internal_level\" : 0, \"internal_resource\" : \"Patient\", \"name\" : /^Teun/i }", mongoQuery.ToString());
        }

        [TestMethod]
        public void TestStringQueryExact()
        {
            var query = new Query().For("Patient").AddParameter("name:exact", "Teun");
            var mongoQuery = createSimpleQuery(query);

            Assert.IsNotNull(mongoQuery);
            AssertQueriesEqual("{ \"internal_level\" : 0, \"internal_resource\" : \"Patient\", \"name\" : \"Teun\" }", mongoQuery.ToString());
        }

        [TestMethod]
        public void TestStringQueryText()
        {
            var query = new Query().For("Patient").AddParameter("name:text", "Teun");
            var mongoQuery = createSimpleQuery(query);

            Assert.IsNotNull(mongoQuery);
            AssertQueriesEqual("{ \"internal_level\" : 0, \"internal_resource\" : \"Patient\", \"namesoundex\" : /^Teun/ }", mongoQuery.ToString());
        }

        [TestMethod]
        public void TestStringQueryChoiceExact()
        {
            var query = new Query().For("Patient").AddParameter("name:exact", "Teun,Truus");
            var mongoQuery = createSimpleQuery(query);

            Assert.IsNotNull(mongoQuery);
            AssertQueriesEqual("{ \"internal_level\" : 0, \"internal_resource\" : \"Patient\", \"name\" : { \"$in\" : [ \"Teun\" , \"Truus\" ] }}", mongoQuery.ToString());
        }

        //[TestMethod]
        //public void TestChainedObservationSubject()
        //{
        //    var query = new Query().For("Observation").AddParameter("subject:Patient.name", "Teun");
        //    var mongoQueries = query.ToMongoQueries();

        //    Assert.IsTrue(mongoQueries.Count > 0);
        //    var chainQuery = (MongoChainQuery)mongoQueries.Where(p => p.GetType() == typeof(MongoChainQuery)).First();
        //    Assert.IsNotNull(chainQuery);

        //    Assert.IsTrue("{ \"subject\" : { \"$in\" : [ \"$keys\" ] } }".EqualsIgnoreSpaces(chainQuery.ToString()));
        //    Assert.IsTrue("{ \"internal_level\" : 0, \"internal_resource\" : \"Patient\", \"name\" : /^Teun/i }".EqualsIgnoreSpaces(chainQuery.SubQueries["$keys"].ToString()));
        //    chainQuery.Resolve("$keys", new List<String>() { "id1", "id2" });
        //    Assert.IsTrue("{ \"subject\" : { \"$in\" : [ \"id1\", \"id2\" ] } }".EqualsIgnoreSpaces(chainQuery.ToString()));

        //    //Assert.AreEqual("{ \"internal_level\" : 0, \"internal_resource\" : \"Observation\", \"subject\" : [ \"$keys\" ] }", mongoQuery.ToString());
        //    //2x querien: een keer op Patient.name, en dan de resulterende ID's in een IN op Observation.subject.
        //    //            Assert.AreEqual("{ \"internal_level\" : 0, \"internal_resource\" : \"Observation\", \"subject\" : /^.*//Teun/i ", mongoQuery.ToString());
        //}
    }

    internal static class TestExtensions
    {
        internal static bool EqualsIgnoreSpaces(this string me, string other)
        {
            return me.Replace(" ", String.Empty).Equals(other.Replace(" ", String.Empty));
        }
    }
}
