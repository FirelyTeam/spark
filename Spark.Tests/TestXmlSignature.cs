/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace Spark.Tests
{
    [TestClass]
    public class TestXmlSignature
    {
        [TestMethod]
        public void TestSigning()
        {
            Bundle b = new Bundle();

            b.Title = "Updates to resource 233";
            b.Id = new Uri("urn:uuid:0d0dcca9-23b9-4149-8619-65002224c3");
            b.LastUpdated = new DateTimeOffset(2012, 11, 2, 14, 17, 21, TimeSpan.Zero);
            b.AuthorName = "Ewout Kramer";

            ResourceEntry<Patient> p = new ResourceEntry<Patient>();
            p.Id = new ResourceIdentity("http://test.com/fhir/Patient/233");
            p.Resource = new Patient();
            p.Resource.Name = new List<HumanName> { HumanName.ForFamily("Kramer").WithGiven("Ewout") };
            b.Entries.Add(p);

            var myAssembly = typeof(TestXmlSignature).Assembly;
            var stream = myAssembly.GetManifestResourceStream("Spark.Tests.spark.pfx");

            var data = new byte[stream.Length];
            stream.Read(data,0,(int)stream.Length);
            var certificate = new X509Certificate2(data);

            var bundleData = FhirSerializer.SerializeBundleToXmlBytes(b);
            var bundleXml = Encoding.UTF8.GetString(bundleData);

            var bundleSigned = XmlSignatureHelper.Sign(bundleXml, certificate);

            Assert.IsTrue(XmlSignatureHelper.IsSigned(bundleSigned));
            Assert.IsTrue(XmlSignatureHelper.VerifySignature(bundleSigned));

            var changedBundle = bundleSigned.Replace("<name>Ewout", "<name>Ewald");
            Assert.AreEqual(bundleSigned.Length, changedBundle.Length);

            Assert.IsFalse(XmlSignatureHelper.VerifySignature(changedBundle));
        }



    }
}
