/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Search.Model;

namespace Spark.Engine.Test.Search;

[TestClass]
public class ReverseIncludeTests
{
    [TestMethod]
    public void TestParseValid()
    {
        ReverseInclude sut = ReverseInclude.Parse("Patient:actor");

        Assert.AreEqual("Patient", sut.ResourceType);
        Assert.AreEqual("actor", sut.SearchPath);
    }
    [TestMethod]
    public void TestParseValidLongerPath()
    {
        ReverseInclude sut = ReverseInclude.Parse("Provenance:target.patient");

        Assert.AreEqual("Provenance", sut.ResourceType);
        Assert.AreEqual("target.patient", sut.SearchPath);
    }
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void TestParseNull()
    {
        _ = ReverseInclude.Parse(null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void TestParseInvalid()
    {
        _ = ReverseInclude.Parse("bla;foo");
    }
}