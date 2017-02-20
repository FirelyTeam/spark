using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Extensions;
using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Test.Extensions
{
    [TestClass]
    public class HttpRequestFhirExtensionsTests
    {
        [TestMethod]
        public void TestGetDateParameter()
        {
            //CK: apparently only works well if you escape at least the '+' sign at the start of the offset (by %2B) .
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("http://spark.furore.com/fhir/Encounter/_history?_since=2017-01-01T00%3A00%3A00%2B01%3A00", UriKind.Absolute));
            var expected = new DateTimeOffset(2017, 1, 1, 0, 0, 0, new TimeSpan(1, 0, 0));
            var actual = httpRequest.GetDateParameter("_since");
            Assert.AreEqual(expected, actual);
        }
    }
}
