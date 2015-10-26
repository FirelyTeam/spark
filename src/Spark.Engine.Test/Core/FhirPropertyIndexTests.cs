using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Test.Search
{
    [TestClass]
    public class FhirPropertyIndexTests
    {
        [TestMethod]
        public void TestGetIndex()
        {
            var index = new FhirPropertyIndex(new List<Type> { typeof(Patient), typeof(Account) });
            Assert.IsNotNull(index);
        }

        [TestMethod]
        public void TestExistingPropertyIsFound()
        {
            var index = new FhirPropertyIndex(new List<Type> { typeof(Patient), typeof(HumanName) });

            var pm = index.findPropertyMapping("Patient", "name");
            Assert.IsNotNull(pm);

            pm = index.findPropertyMapping("HumanName", "given");
            Assert.IsNotNull(pm);
        }

        [TestMethod]
        public void TestNonExistingPropertyReturnsNull()
        {
            var index = new FhirPropertyIndex(new List<Type> { typeof(Patient), typeof(Account) });

            var pm = index.findPropertyMapping("TypeNotPresent", "subject");
            Assert.IsNull(pm);

            pm = index.findPropertyMapping("Patient", "property_not_present");
            Assert.IsNull(pm);
        }
    }
}
