/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Model;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Test.Extensions;

[TestClass]
public class SearchParameterExtensionsTests
{
    [TestMethod]
    public void TestSetPropertyPathWithSinglePath()
    {
        SearchParameter sut = new SearchParameter
        {
            Base = [VersionIndependentResourceTypesAll.Appointment]
        };

        sut.SetPropertyPath(["Appointment.participant.actor"]);

        Assert.AreEqual("//participant/actor", sut.Xpath);
    }

    [TestMethod]
    public void TestSetPropertyPathWithMultiplePath()
    {
        SearchParameter sut = new SearchParameter
        {
            Base = [VersionIndependentResourceTypesAll.AuditEvent]
        };
        sut.SetPropertyPath(["AuditEvent.participant.reference", "AuditEvent.object.reference"]);

        Assert.AreEqual("//participant/reference | //object/reference", sut.Xpath);
    }

    [TestMethod]
    public void  TestGetPropertyPathWithSinglePath()
    {
        SearchParameter sut = new SearchParameter
        {
            Xpath = "//participant/actor"
        };

        var paths = sut.GetPropertyPath();
        Assert.AreEqual(1, paths.Count());
        Assert.IsTrue(paths.Contains("participant.actor"));
    }

    [TestMethod]
    public void TestGetPropertyPathWithMultiplePath()
    {
        SearchParameter sut = new SearchParameter
        {
            Xpath = "//participant/reference | //object/reference"
        };

        var paths = sut.GetPropertyPath();
        Assert.AreEqual(2, paths.Count());
        Assert.IsTrue(paths.Contains("participant.reference"));
        Assert.IsTrue(paths.Contains("object.reference"));
    }

    [TestMethod]
    public void TestSetPropertyPathWithPredicate()
    {
        SearchParameter sut = new SearchParameter
        {
            Base = [VersionIndependentResourceTypesAll.Slot]
        };
        sut.SetPropertyPath(["Slot.extension(url=http://foo.com/myextension).valueReference"]);

        Assert.AreEqual("//extension(url=http://foo.com/myextension)/valueReference", sut.Xpath);
    }

    [TestMethod]
    public void TestGetPropertyPathWithPredicate()
    {
        SearchParameter sut = new SearchParameter
        {
            Xpath = "//extension(url=http://foo.com/myextension)/valueReference"
        };

        var paths = sut.GetPropertyPath();
        Assert.AreEqual(1, paths.Count());
        Assert.AreEqual(@"extension(url=http://foo.com/myextension).valueReference", paths[0]);
    }

    [TestMethod]
    public void TestMatchExtension()
    {
        var input = "//extension(url=http://foo.com/myextension)/valueReference";
        var result = SearchParameterExtensions.XPathPattern.Match(input).Value;
        Assert.AreEqual(input, result);
    }
}
