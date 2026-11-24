/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Core;
using System.Linq;
using Hl7.Fhir.Model;

namespace Spark.Engine.Test.Core;

[TestClass]
public class FhirModelTests
{
    private static FhirModel sut;

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
        sut = new FhirModel();
    }

    [TestMethod]
    public void TestCompartments()
    {
        var actual = sut.FindCompartmentInfo(ResourceType.Patient);

        Assert.IsNotNull(actual);
        Assert.IsTrue(actual.ReverseIncludes.Any());
    }

    [TestMethod]
    public void SearchParameter_Id_Is_Of_Type_Token()
    {
        var actual = sut.FindSearchParameter(ResourceType.Patient, "_id");

        Assert.IsNotNull(actual);
        Assert.AreEqual(SearchParamType.Token, actual.Type);
    }

    [TestMethod]
    public void FindSearchParameters_UsesGeneratedGenericParameterDefinitions()
    {
        var genericParameters = sut.FindSearchParameters("Patient")
            .Where(parameter => new[] { "_id", "_lastUpdated", "_tag", "_profile", "_security", "_source" }.Contains(parameter.Name))
            .ToDictionary(parameter => parameter.Name, parameter => parameter.Type);

        Assert.AreEqual(6, genericParameters.Count);
        Assert.AreEqual(SearchParamType.Token, genericParameters["_id"]);
        Assert.AreEqual(SearchParamType.Date, genericParameters["_lastUpdated"]);
        Assert.AreEqual(SearchParamType.Token, genericParameters["_tag"]);
        Assert.AreEqual(SearchParamType.Uri, genericParameters["_profile"]);
        Assert.AreEqual(SearchParamType.Token, genericParameters["_security"]);
        Assert.AreEqual(SearchParamType.Uri, genericParameters["_source"]);
    }
}