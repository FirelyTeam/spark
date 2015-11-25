using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Test.Extensions
{
    [TestClass]
    public class SearchParameterExtensionsTests
    {
        [TestMethod]
        public void TestSetPropertyPathWithSinglePath()
        {
            SearchParameter sut = new SearchParameter();
            sut.Base = "Appointment";
            sut.SetPropertyPath(new string[] { "Appointment.participant.actor" });

            Assert.AreEqual("//participant/actor", sut.Xpath);
        }

        [TestMethod]
        public void TestSetPropertyPathWithMultiplePath()
        {
            SearchParameter sut = new SearchParameter();
            sut.Base = "AuditEvent";
            sut.SetPropertyPath(new string[] { "AuditEvent.participant.reference", "AuditEvent.object.reference" });

            Assert.AreEqual("//participant/reference | //object/reference", sut.Xpath);
        }

        [TestMethod]
        public void  TestGetPropertyPathWithSinglePath()
        {
            SearchParameter sut = new SearchParameter();
            sut.Xpath = "//participant/actor";

            var paths = sut.GetPropertyPath();
            Assert.AreEqual(1, paths.Count());
            Assert.IsTrue(paths.Contains("participant.actor"));
        }

        [TestMethod]
        public void TestGetPropertyPathWithMultiplePath()
        {
            SearchParameter sut = new SearchParameter();
            sut.Xpath = "//participant/reference | //object/reference";

            var paths = sut.GetPropertyPath();
            Assert.AreEqual(2, paths.Count());
            Assert.IsTrue(paths.Contains("participant.reference"));
            Assert.IsTrue(paths.Contains("object.reference"));
        }
    }
}
