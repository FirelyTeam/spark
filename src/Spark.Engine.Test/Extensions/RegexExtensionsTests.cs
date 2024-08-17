/* 
 * Copyright (c) 2016-2018, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using Spark.Engine.Extensions;

namespace Spark.Engine.Test.Extensions
{
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
}
