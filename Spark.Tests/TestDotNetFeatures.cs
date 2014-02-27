using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Specialized;
using Spark.Service;
using Hl7.Fhir.Support;

namespace SparkTests
{
    [TestClass]
    public class TestDotNetFeatures
    {
        [TestMethod]
        public void TestDateTimeParsing()
        {
            DateTimeOffset value;

            Assert.IsTrue(Util.TryParseIsoDateTime("1972-01-30T14:34:12+02:00", out value));
        }

        [TestMethod]
        public void TestManipulateUri()
        {
            Uri testUri = new Uri("http://ip-0a7a5abe:14236/fhir/person?$format=json", UriKind.Absolute);
            Uri serviceUrl = new Uri("http://fhir.furore.com/fhir");

            Uri newUri = new Uri(serviceUrl, testUri.PathAndQuery);
            Assert.AreEqual("http://fhir.furore.com/fhir/person?$format=json", newUri.ToString());

            Uri emptyUri = new Uri("/", UriKind.Relative);

        }

        [TestMethod]
        public void TestNameValueCollectionWithMultipleValuesOnOneKey()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("a", "value a1");
            nvc.Add("a", "value a2");

            Assert.AreEqual(1, nvc.AllKeys.Count());
            Assert.AreEqual(2, nvc.GetValues("a").Count());
        }

        [TestMethod]
        public void TestNameValueCollectionValuesAreNotUnique()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("a", "value a1");
            nvc.Add("a", "value a1"); //the same values again

            Assert.AreEqual(1, nvc.AllKeys.Count());
            Assert.AreEqual(2, nvc.GetValues("a").Count(), "If there was only 1 value, NameValueCollection would enforce uniqueness. Apparently it does not.");
            Assert.AreEqual(1, nvc.GetValues("a").Distinct().Count(), "If we apply a Distinct afterwards, I would expect to have only one value left.");
        }

        [TestMethod]
        public void AssureNavigateToRelativeResource()
        {
            var rl = new ResourceLocation("http://hl7.org/fhir/patient/@1");

            var rln = rl.NavigateTo("@2");
            Assert.AreEqual("patient/@2", rln.OperationPath.ToString());

            rln = rl.NavigateTo("../observation/@3");
            Assert.AreEqual("observation/@3", rln.OperationPath.ToString());
        }

    }
}
