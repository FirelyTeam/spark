using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spark.Engine.Test.Extensions
{
    [TestClass]
    public class OperationOutcomeExtensionsTests
    {
        [TestMethod]
        public void CanConvertFhirPathExpressionToXPathExpression()
        {
            var fhirPathExpression = "Patient.name[0].family";
            var expected = "f:Patient/f:name[0]/f:family";

            var actual = OperationOutcomeExtensions.ConvertToXPathExpression(fhirPathExpression);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CanResolveToFhirPathExpression()
        {
            var resourceType = typeof(Patient);
            var expression = "Name[0].FamilyElement";
            var expected = "Patient.name[0].family";

            var actual = OperationOutcomeExtensions.ResolveToFhirPathExpression(resourceType, expression);

            Assert.AreEqual(expected, actual);
        }
    }
}
