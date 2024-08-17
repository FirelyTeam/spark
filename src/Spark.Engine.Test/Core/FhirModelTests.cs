/* 
 * Copyright (c) 2016-2018, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
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
