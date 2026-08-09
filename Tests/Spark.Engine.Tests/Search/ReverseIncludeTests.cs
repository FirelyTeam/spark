/*
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Xunit;
using Spark.Engine.Search.Model;
using System;

namespace Spark.Engine.Tests.Search;

public class ReverseIncludeTests
{
    [Fact]
    public void TestParseValid()
    {
        ReverseInclude sut = ReverseInclude.Parse("Patient:actor");

        Assert.Equal("Patient", sut.ResourceType);
        Assert.Equal("actor", sut.SearchPath);
    }
    [Fact]
    public void TestParseValidLongerPath()
    {
        ReverseInclude sut = ReverseInclude.Parse("Provenance:target.patient");

        Assert.Equal("Provenance", sut.ResourceType);
        Assert.Equal("target.patient", sut.SearchPath);
    }
    [Fact]
    public void TestParseNull()
    {
        Assert.Throws<ArgumentNullException>(() => _ = ReverseInclude.Parse(null));
    }

    [Fact]
    public void TestParseInvalid()
    {
        Assert.Throws<ArgumentException>(() => _ = ReverseInclude.Parse("bla;foo"));
    }
}
