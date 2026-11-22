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
}
