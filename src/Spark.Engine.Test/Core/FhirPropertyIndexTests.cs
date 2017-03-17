﻿using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var index = new FhirPropertyIndex(_fhirModel, new List<Type> { typeof(ClinicalImpression), typeof(CodeableConcept) });

            var pm = index.findPropertyInfo("ClinicalImpression", "triggerCodeableConcept");
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
