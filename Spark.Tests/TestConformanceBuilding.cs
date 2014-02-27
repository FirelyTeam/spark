using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Service;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Spark.Support;
using System.Diagnostics;

namespace SparkTests
{
    [TestClass]
    public class TestConformanceBuilding
    {
        [TestMethod]
        public void TestBuildConformance()
        {
            var b = ConformanceBuilder.Build();

            var xml = Hl7.Fhir.Serializers.FhirSerializer.SerializeResourceToXml(b);
        }
    }
}
