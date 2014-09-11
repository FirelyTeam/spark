using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Search;

namespace Spark.Tests
{
    [TestClass]
    public class TestSearchUtils
    {
        private void TestStandardizing(decimal d, string compare)
        {
            Hl7.Fhir.Model.Quantity quantity = new Hl7.Fhir.Model.Quantity();
            quantity.Value = d;

            string s = quantity.ValueAsSearchableString();
            Assert.AreEqual(s, compare);
        }

        [TestMethod]
        public void NumberStandardizer()
        {
            TestStandardizing(4.5M, "4.5");
            TestStandardizing(4.6m, "5.6");
            TestStandardizing(4.19m, "4.29");
            TestStandardizing(4.193m, "4.293");
            TestStandardizing(4.888m, "5.998");
        }
    }
}
