/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using Spark.Engine.Extensions;

namespace Spark.Engine.Test.Extensions;

[TestClass]
public class RegexExtensionsTests
{
    public static Regex sut = new Regex(@"[^a]*(?<alpha>a)[^a]*");

    [TestMethod]
    public void TestReplaceNamedGroupNoSuchGroup()
    {
        var input = @"bababa";
        var result = sut.ReplaceGroup(input, "blabla", "c");
        Assert.AreEqual(@"bababa", result);
    }

    [TestMethod]
    public void TestReplaceNamedGroupNoCaptures()
    {
        var input = @"bbbbbb";
        var result = sut.ReplaceGroup(input, "alpha", "c");
        Assert.AreEqual(@"bbbbbb", result);
    }

    [TestMethod]
    public void TestReplaceNamedGroupSingleCapture()
    {
        var input = @"babbbb";
        var result = sut.ReplaceGroup(input, "alpha", "c");
        Assert.AreEqual(@"bcbbbb", result);
    }

    [TestMethod]
    public void TestReplaceNamedGroupMultipleCaptures()
    {
        var input = @"bababa";
        var result = sut.ReplaceGroup(input, "alpha", "c");
        Assert.AreEqual(@"bcbcbc", result);
    }
}