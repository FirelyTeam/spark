using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Controllers;
using Spark.Core;

namespace Spark.Tests
{
    [TestClass]
    public class RequestValidatorTest
    {
        [TestMethod]
        [ExpectedException (typeof (SparkException)) ]
        public void InvalidIdShouldReturnFalse()
        {
            RequestValidator.ValidateIdPattern("Diagnosticreport-example-dxa");
            Assert.Fail("Should report false on 'Diagnosticreport-example-dxa', since it contains a capital letter");
        }
    }
}
