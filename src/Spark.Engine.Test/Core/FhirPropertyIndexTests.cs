/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly)
 * Copyright (c) 2018-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;

namespace Spark.Engine.Test.Search
{
    [TestClass]
    public class FhirPropertyIndexTests
    {
        private static IFhirModel _fhirModel = new FhirModel();

        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestMethod]
        public void TestGetIndex()
        {
            var index = new FhirPropertyIndex(_fhirModel, new List<Type> { typeof(Patient), typeof(Account) });
            Assert.IsNotNull(index);
        }

        [TestMethod]
        public void TestExistingPropertyIsFound()
        {
            var index = new FhirPropertyIndex(_fhirModel, new List<Type> { typeof(Patient), typeof(HumanName) });

            var pm = index.findPropertyInfo("Patient", "name");
            Assert.IsNotNull(pm);

            pm = index.findPropertyInfo("HumanName", "given");
            Assert.IsNotNull(pm);
        }

        [TestMethod]
        public void TestTypedNameIsFound()
        {
            var index = new FhirPropertyIndex(_fhirModel, new List<Type> { typeof(ClinicalImpression), typeof(Period) });

            var pm = index.findPropertyInfo("ClinicalImpression", "effectivePeriod");
            Assert.IsNotNull(pm);
        }

        [TestMethod]
        public void TestNonExistingPropertyReturnsNull()
        {
            var index = new FhirPropertyIndex(_fhirModel, new List<Type> { typeof(Patient), typeof(Account) });

            var pm = index.findPropertyInfo("TypeNotPresent", "subject");
            Assert.IsNull(pm);

            pm = index.findPropertyInfo("Patient", "property_not_present");
            Assert.IsNull(pm);
        }
    }
}
