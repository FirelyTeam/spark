using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Extensions;
using System;
using System.Net.Http;
using Hl7.Fhir.Rest;
using Spark.Engine.Utility;

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
            var actual = FhirParameterParser.ParseDateParameter(httpRequest.GetParameter("_since"));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RequestSummary_SummaryTypeDefaultIsFalse()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/fhir/Patient");
            var expected = SummaryType.False;
            var actual = request.RequestSummary();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RequestSummary_SummaryTypeIsText()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/fhir/Patient?_summary=text");
            var expected = SummaryType.Text;
            var actual = request.RequestSummary();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RequestSummary_SummaryTypeIsData()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/fhir/Patient?_summary=data");
            var expected = SummaryType.Data;
            var actual = request.RequestSummary();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RequestSummary_SummaryTypeIsTrue()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/fhir/Patient?_summary=true");
            var expected = SummaryType.True;
            var actual = request.RequestSummary();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RequestSummary_SummaryTypeIsFalse()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/fhir/Patient?_summary=false");
            var expected = SummaryType.False;
            var actual = request.RequestSummary();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RequestSummary_SummaryTypeIsCount()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/fhir/Patient?_summary=count");
            var expected = SummaryType.Count;
            var actual = request.RequestSummary();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RequestSummary_SummaryTypeIsFalseWhen_summaryIsEmpty()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/fhir/Patient?_summary=");
            var expected = SummaryType.False;
            var actual = request.RequestSummary();
            Assert.AreEqual(expected, actual);
        }
    }
}
