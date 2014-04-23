using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Config;
using System.Configuration;
using Spark.Support;
using Spark.Service;
using Spark.Search;
using Spark.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Search;

namespace Spark.Tests
{
    [TestClass]
    public class FhirIndexTest
    {
        protected static FhirIndex index;

        [ClassInitialize]
        public static void Import(TestContext unused)
        {
            Dependencies.Register();
            Settings.AppSettings = ConfigurationManager.AppSettings;

            FhirMaintainanceService maintainance = Factory.GetFhirMaintainceService();
            maintainance.Initialize();

            index = Factory.GetIndex();

        }

        [TestMethod]
        public void String_FindsResourceOnExactValue()
        {
            // Default search = partial from the start
            var q = new Query().For("Patient").Where("family=Mckinney");
            var r = index.Search(q);
            Assert.IsTrue(r.Has("Patient/76"));
        }

        [TestMethod]
        public void String_DoesNotFindResourceOnWronglySpelledValue()
        {
            var q = new Query().For("Patient").Where("family=Mckinley");
            var r = index.Search(q);
            Assert.IsFalse(r.Has("Patient/76"));
        }

        [TestMethod]
        public void String_FindsResourceOnValueWithDifferentCapitialization()
        {
            var q = new Query().For("Patient").Where("family=McKinney");
            var r = index.Search(q);
            Assert.IsTrue(r.Has("Patient/76"));
        }

        [TestMethod]
        public void String_DoesNotFindPatientOnLastPartOfValue()
        {
            var q = new Query().For("Patient").Where("family=kinney");
            var r = index.Search(q);
            Assert.IsFalse(r.Has("Patient/76"));
        }

        [TestMethod]
        public void String_FindsResourceOnFirstPartOfValue()
        {
            var q = new Query().For("Patient").Where("family=Mckinn");
            var r = index.Search(q);
            Assert.IsTrue(r.Has("Patient/76"));
        }

        [TestMethod]
        public void String_DoesNotFindResourceOnMiddlePartOfValue()
        {
            var q = new Query().For("Patient").Where("family=kinne");
            var r = index.Search(q);
            Assert.IsFalse(r.Has("Patient/76"));
        }

        [TestMethod]
        public void StringExact_FindsResourceOnExactValue()
        {
            var q = new Query().For("Patient").Where("family:exact=Mckinney");
            var r = index.Search(q);
            Assert.IsTrue(r.Has("Patient/76"));
        }

        [TestMethod]
        public void StringExact_DoesNotFindResourceOnWronlySpelledValue()
        {
            var q = new Query().For("Patient").Where("family:exact=Mckinley");
            var r = index.Search(q);
            Assert.IsFalse(r.Has("Patient/76"));
        }

        [TestMethod]
        public void StringExact_DoesNotFindResourceOnValueWithDifferentCapitalization()
        {
            var q = new Query().For("Patient").Where("family:exact=McKinney");
            var r = index.Search(q);
            Assert.IsFalse(r.Has("Patient/76"));
        }

        [TestMethod]
        public void StringExact_DoesNotFindResourceOnLastPartOfValue()
        {
            var q = new Query().For("Patient").Where("family:exact=kinney");
            var r = index.Search(q);
            Assert.IsFalse(r.Has("Patient/76"));
        }

        [TestMethod]
        public void StringExact_DoesNotFindResourceOnFirstPartOfValue()
        {
            var q = new Query().For("Patient").Where("family:exact=Mckinn");
            var r = index.Search(q);
            Assert.IsFalse(r.Has("Patient/76"));
        }

        [TestMethod]
        public void StringExact_DoesNotFindResourceOnMiddlePartOfValue()
        {
            var q = new Query().For("Patient").Where("family:exact=kinne");
            var r = index.Search(q);
            Assert.IsFalse(r.Has("Patient/76"));
        }

        [TestMethod]
        public void Reference_FindsResourceOnReferenceId()
        {
            var q = new Query().For("Patient").Where("given=ned").Where("provider=Organization/hl7");
            var results = index.Search(q);
            //results = index.Search("Patient", "given=\"ned\"&provider=Organization/hl7 ");
            Assert.IsTrue(results.Count == 1);
        }

        [TestMethod]
        public void String_FindsResourceOnTwoExactValues()
        {
            var q = new Query().For("Patient").Where("given=nancy").Where("family=nuclear");
            var results = index.Search(q);
            //results = index.Search("Patient", "given=\"nancy\"&family=\"nuclear\"");
            Assert.IsTrue(results.Count == 1);
        }

        [TestMethod]
        public void ChainWithModifierToString_FindsResourceOnFirstPartOfValue()
        {
            var q = new Query().For("Patient").Where("given=nancy").Where("provider:Organization.name=health");
            var results = index.Search(q);
            //results = index.Search("Patient", "given=\"nancy\"&provider:Organization.name:partial=\"health\"");
            Assert.IsTrue(results.Count == 1);
        }

        [TestMethod]
        public void ChainWithModifierToString_DoesNotFindResourceOnWronglySpelledValue()
        {
            var q = new Query().For("Patient").Where("given=nancy").Where("provider:Organization.name=healthy");
            var results = index.Search(q);
            //results = index.Search("Patient", "given=\"ned\"&provider:Organization.name:partial=\"healthy\"");
            Assert.IsTrue(results.Count == 0);
        }

        [TestMethod]
        public void ChainWithModifierToStringExact_FindsResourceOnExactValue()
        {
            var q = new Query().For("Patient").Where("given=nancy").Where("provider:Organization.name:exact=Health Level Seven International");
            var results = index.Search(q);
            //results = index.Search("Patient", "given=\"ned\"&provider:Organization.name:exact=\"Health Level Seven International\"");
            Assert.IsTrue(results.Count == 1);
        }

        [TestMethod]
        public void ChainWithModifierToStringExact_DoesNotFindResourceOnValueWithDifferentCapitalization()
        {
            var q = new Query().For("Patient").Where("given=nancy").Where("provider:Organization.name:exact=Health Level seven International");
            var results = index.Search(q);
            //results = index.Search("Patient", "given=\"ned\"&provider:Organization.name:exact=\"Health Level seven International\"");
            Assert.IsTrue(results.Count == 0);
        }

        [TestMethod]
        public void ChainWithModifierToStringExact_DoesNotFindResourceOnFirstPartOfValue()
        {
            var q = new Query().For("Patient").Where("given=nancy").Where("provider:Organization.name:exact=Health Level Seven Inter");
            var results = index.Search(q);
            //results = index.Search("Patient", "given=\"ned\"&provider:Organization.name:exact=\"Health Level Seven Inter\"");
            Assert.IsTrue(results.Count == 0);
        }

        [TestMethod]
        public void ChainWithModifierToToken_FindsResourceOnTokenCode()
        {
            // THE  WORKS
            var q = new Query().For("Condition").Where("subject:Patient.identifier=12345");
            var results = index.Search(q);
            //results = index.Search("Condition", "subject.identifier=12345");
            Assert.IsTrue(results.Has("Condition/example"));
            Assert.IsTrue(results.Has("Condition/example2"));
            Assert.IsTrue(results.Count > 2);
        }

        [TestMethod]
        public void TripleChainToId_FindsResource()
        {
             //deeper chains:
            //CarePlan.Condition -> Problem
            //Problem.Subject -> Patient
            //Patient.Provider -> Organization 
            //Organization._id -> organization/1 (liever had ik .name gehad, maar /1 bestaat niet.            
            var q = new Query().For("CarePlan").Where("condition.patient.provider._id=organization/1");
            var results = index.Search(q);

             Assert.IsTrue(results.Count == 1);
        }

        [TestMethod]
        public void TripleChainToString_FindsResourceOnExactValue()
        {
            //deeper chains:
            //CarePlan.Condition -> Problem
            //Problem.Subject -> Patient
            //Patient.Provider -> Organization 
            //Organization._id -> organization/1 (liever had ik .name gehad, maar /1 bestaat niet.            
            var q = new Query().For("CarePlan").Where("condition.asserter.provider.name=Gastroenterology");
            var results = index.Search(q);

            Assert.IsTrue(results.Count == 1);
            //TODO: Because we don't filter on the allowed referenced resource types yet (see CriteriaMongoExtensions.GetTargetedReferenceTypes),
            //we also get into DiagnosticReport.name, and that is a token parameter - which is still unsupported.
        }
    }
}
