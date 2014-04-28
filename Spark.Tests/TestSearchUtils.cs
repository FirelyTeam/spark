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
            string s = Units.DecimalSearchable(d);
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
