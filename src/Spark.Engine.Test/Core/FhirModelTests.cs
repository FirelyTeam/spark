/* 
 * Copyright (c) 2016-2018, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Core;
using System.Linq;
using Hl7.Fhir.Model;

namespace Spark.Engine.Test.Core
{
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
    }
}
