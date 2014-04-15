using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hl7.Fhir.Model;
using Hl7.Fhir.Search;
using Spark.Search;

namespace Spark.Tests
{
    [TestClass]
    public class TestCriteriaMongoExtensions
    {
        [TestMethod]
        public void TestNumberQuery()
        {
            var query = new Query().For("ImagingStudy").AddParameter("size", "10");
            var mongoQuery = query.ToQuery();

            Assert.IsNotNull(mongoQuery);
            Assert.AreEqual("{ \"internal_level\" : 0, \"internal_resource\" : \"ImagingStudy\", \"size\" : \"10\" }", mongoQuery.ToString());
        }

        [TestMethod]
        public void TestStringQueryDefault() //default is partial from start
        {
            var query = new Query().For("Patient").AddParameter("name", "Teun");
            var mongoQuery = query.ToQuery();

            Assert.IsNotNull(mongoQuery);
            Assert.AreEqual("{ \"internal_level\" : 0, \"internal_resource\" : \"Patient\", \"name\" : /^Teun/i }", mongoQuery.ToString());
        }

        [TestMethod]
        public void TestStringQueryExact()
        {
            var query = new Query().For("Patient").AddParameter("name:exact", "Teun");
            var mongoQuery = query.ToQuery();

            Assert.IsNotNull(mongoQuery);
            Assert.AreEqual("{ \"internal_level\" : 0, \"internal_resource\" : \"Patient\", \"name\" : \"Teun\" }", mongoQuery.ToString());
        }

        [TestMethod]
        public void TestStringQueryText()
        {
            var query = new Query().For("Patient").AddParameter("name:text", "Teun");
            var mongoQuery = query.ToQuery();

            Assert.IsNotNull(mongoQuery);
            Assert.AreEqual("{ \"internal_level\" : 0, \"internal_resource\" : \"Patient\", \"namesoundex\" : /^Teun/ }", mongoQuery.ToString());
        }

        [TestMethod]
        public void TestStringQueryChoiceExact()
        {
            var query = new Query().For("Patient").AddParameter("name:exact", "Teun,Truus");
            var mongoQuery = query.ToQuery();

            Assert.IsNotNull(mongoQuery);
            Assert.AreEqual("{ \"internal_level\" : 0, \"internal_resource\" : \"Patient\", \"$or\" : [{ \"name\" : \"Teun\" }, { \"name\" : \"Truus\" }] }", mongoQuery.ToString());
        }

        [TestMethod]
        public void TestChainedObservationSubject()
        {
            var query = new Query().For("Observation").AddParameter("subject:Patient.name", "Teun");
            var mongoQuery = query.ToQuery();

            Assert.IsNotNull(mongoQuery);
            //2x querien: een keer op Patient.name, en dan de resulterende ID's in een IN op Observation.subject.
            Assert.AreEqual("{ \"internal_level\" : 0, \"internal_resource\" : \"Observation\", \"subject\" : /^.*//Teun/i ", mongoQuery.ToString());
        }
    }
}
