/* 
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Utility;
using System;

namespace Spark.Engine.Test.Utility;

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
        const string expression = "Name[0].FamilyElement";
        const string expected = "Patient.name[0].family";

        var actual = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Resolve_Questionnaire_ItemElement_Hierarchy_Test()
    {
        Type resourceType = typeof(Questionnaire);
        const string expression = "Item[0].Item[3].Item[0].Item[3]";
        const string expected = "Questionnaire.item[0].item[3].item[0].item[3]";

        string resolvedExpression = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

        Assert.AreEqual(expected, resolvedExpression);
    }

    [TestMethod]
    public void Resolve_Questionnaire_RequriedElement_In_ItemElement_Hierarchy_Test()
    {
        Type resourceType = typeof(Questionnaire);
        const string expression = "Item[0].Item[3].Item[0].Item[3].RequiredElement";
        const string expected = "Questionnaire.item[0].item[3].item[0].item[3].required";

        var resolvedExpression = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

        Assert.AreEqual(expected, resolvedExpression);
    }

    [TestMethod]
    public void Resolve_Questionnaire_Initial_In_ItemElement_Hierarchy_Test()
    {
        const string expression = "Item[0].Item[3].Item[0].Item[3].Initial";
        const string expected = "Questionnaire.item[0].item[3].item[0].item[3].initial";

        string resolvedExpression = FhirPathUtil.ResolveToFhirPathExpression(typeof(Questionnaire), expression);

        Assert.AreEqual(expected, resolvedExpression);
    }

    [TestMethod]
    public void Resolve_Patient_Communication_Language_Test()
    {
        Type resourceType = typeof(Patient);
        const string expression = "Communication[0].Language";
        const string expected = "Patient.communication[0].language";

        string resolvedExpression = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

        Assert.AreEqual(expected, resolvedExpression);
    }
}
