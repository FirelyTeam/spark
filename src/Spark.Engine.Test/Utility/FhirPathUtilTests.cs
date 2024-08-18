/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Utility;
using System;

namespace Spark.Engine.Test.Utility
{
    [TestClass]
    public class FhirPathUtilTests
    {
        [TestMethod]
        public void Can_Convert_FhirPathExpression_To_XPathExpression_Test()
        {
            var fhirPathExpression = "Patient.name[0].family";
            var expected = "f:Patient/f:name[0]/f:family";

            var actual = FhirPathUtil.ConvertToXPathExpression(fhirPathExpression);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Resolve_Patient_FamilyElement_Test()
        {
            var resourceType = typeof(Patient);
            var expression = "Name[0].FamilyElement";
            var expected = "Patient.name[0].family";

            var actual = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Resolve_Questionnaire_ItemElement_Hierarchy_Test()
        {
            Type resourceType = typeof(Questionnaire);
            var expression = ModelInfo.Version == "1.0.2"
                ? "Group.Question[3].Group[0].Question[3]"
                : "Item[0].Item[3].Item[0].Item[3]";
            var expected = ModelInfo.Version == "1.0.2"
                ? "Questionnaire.group.question[3].group[0].question[3]"
                : "Questionnaire.item[0].item[3].item[0].item[3]";

            string resolvedExpression = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

            Assert.AreEqual(expected, resolvedExpression);
        }

        [TestMethod]
        public void Resolve_Questionnaire_RequriedElement_In_ItemElement_Hierarchy_Test()
        {
            Type resourceType = typeof(Questionnaire);
            var expression = ModelInfo.Version == "1.0.2"
                ? "Group.Question[3].Group[0].Question[3].RequiredElement"
                : "Item[0].Item[3].Item[0].Item[3].RequiredElement";
            var expected = ModelInfo.Version == "1.0.2"
                ? "Questionnaire.group.question[3].group[0].question[3].required"
                : "Questionnaire.item[0].item[3].item[0].item[3].required";

            var resolvedExpression = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

            Assert.AreEqual(expected, resolvedExpression);
        }

        [TestMethod]
        public void Resolve_Questionnaire_Initial_In_ItemElement_Hierarchy_Test()
        {
            Type resourceType = typeof(Questionnaire);
            // NOTE: Initial does not exist in DSTU2
            string expression = ModelInfo.Version == "1.0.2"
                ? "Group.Question[3].Group[0].Question[3].TextElement"
                : "Item[0].Item[3].Item[0].Item[3].Initial";
            string expected = ModelInfo.Version == "1.0.2"
                ? "Questionnaire.group.question[3].group[0].question[3].text"
                : "Questionnaire.item[0].item[3].item[0].item[3].initial";

            string resolvedExpression = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

            Assert.AreEqual(expected, resolvedExpression);
        }

        [TestMethod]
        public void Resolve_Patient_Communication_Language_Test()
        {
            Type resourceType = typeof(Patient);
            string expression = "Communication[0].Language";

            string resolvedExpression = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

            Assert.AreEqual("Patient.communication[0].language", resolvedExpression);
        }
    }
}
