/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Xunit;
using Spark.Engine.Extensions;
using System.Linq;
using SearchParameter = Spark.Engine.Model.SearchParameter;

namespace Spark.Engine.Tests.Extensions;

public class SearchParameterExtensionsTests
{
    [Fact]
    public void TestSetPropertyPathWithSinglePath()
    {
        SearchParameter sut = new SearchParameter
        {
            Base = [VersionIndependentResourceTypesAll.Appointment]
        };

        sut.SetPropertyPath(["Appointment.participant.actor"]);

        Assert.Equal("//participant/actor", sut.Xpath);
    }

    [Fact]
    public void TestSetPropertyPathWithMultiplePath()
    {
        SearchParameter sut = new SearchParameter
        {
            Base = [VersionIndependentResourceTypesAll.AuditEvent]
        };
        sut.SetPropertyPath(["AuditEvent.participant.reference", "AuditEvent.object.reference"]);

        Assert.Equal("//participant/reference | //object/reference", sut.Xpath);
    }

    [Fact]
    public void  TestGetPropertyPathWithSinglePath()
    {
        SearchParameter sut = new SearchParameter
        {
            Xpath = "//participant/actor"
        };

        var paths = sut.GetPropertyPath();
        Assert.Equal(1, paths.Count());
        Assert.True(paths.Contains("participant.actor"));
    }

    [Fact]
    public void TestGetPropertyPathWithMultiplePath()
    {
        SearchParameter sut = new SearchParameter
        {
            Xpath = "//participant/reference | //object/reference"
        };

        var paths = sut.GetPropertyPath();
        Assert.Equal(2, paths.Count());
        Assert.True(paths.Contains("participant.reference"));
        Assert.True(paths.Contains("object.reference"));
    }

    [Fact]
    public void TestSetPropertyPathWithPredicate()
    {
        SearchParameter sut = new SearchParameter
        {
            Base = [VersionIndependentResourceTypesAll.Slot]
        };
        sut.SetPropertyPath(["Slot.extension(url=http://foo.com/myextension).valueReference"]);

        Assert.Equal("//extension(url=http://foo.com/myextension)/valueReference", sut.Xpath);
    }

    [Fact]
    public void TestGetPropertyPathWithPredicate()
    {
        SearchParameter sut = new SearchParameter
        {
            Xpath = "//extension(url=http://foo.com/myextension)/valueReference"
        };

        var paths = sut.GetPropertyPath();
        Assert.Equal(1, paths.Count());
        Assert.Equal(@"extension(url=http://foo.com/myextension).valueReference", paths[0]);
    }

    [Fact]
    public void TestMatchExtension()
    {
        var input = "//extension(url=http://foo.com/myextension)/valueReference";
        var result = SearchParameterExtensions.XPathPattern.Match(input).Value;
        Assert.Equal(input, result);
    }
}
