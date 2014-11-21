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
using System.Collections.Generic;

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

            FhirMaintenanceService maintainance = Factory.GetFhirMaintenanceService();
            maintainance.Initialize(false);

            index = Spark.Search.MongoSearchFactory.GetIndex();

            AddTaggedPatient();
        }

        private static string _otherTag = randomTag();

        private static void AddTaggedPatient()
        {
            IFhirStore store = Spark.Store.MongoStoreFactory.GetMongoFhirStore();
            var patient = new Patient();
            patient.Id = "Patient/tagged";
            patient.Name = new List<HumanName>();
            patient.Name.Add(new HumanName() {Given = new string[]{"Truus"}, Family = new string[]{"Tagged"}});
            ResourceEntry patientRE = ResourceEntry.Create(patient);
            patientRE.Id = new Uri("Patient/tagged", UriKind.Relative);
            patientRE.SelfLink = new Uri("Patient/tagged", UriKind.Relative);
            patientRE.Tags.Add(new Tag(_otherTag, Tag.FHIRTAGSCHEME_GENERAL, "dummy"));
            store.Add(patientRE);
            index.Process(patientRE);
        }

        private static string randomTag()
        {
            string s = new Random().Next().ToString();
            return string.Format("http://othertag{0}.hl7.nl", s);
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
        public void String_IgnoresAndReportsInvalidModifier()
        {
            var q = new Query().For("Patient").Where("family:bla=Mckinney");
            var r = index.Search(q);
            Assert.IsTrue(r.HasIssues);
            Assert.AreEqual(String.Empty, r.UsedCriteria);
            Assert.IsTrue(r.Count > 0); //All Patient resources should be found.
        }

        [TestMethod]
        public void String_IgnoresAndReportsInvalidOperator()
        {
            var q = new Query().For("Patient").Where("family=>Mckinney");
            var r = index.Search(q);
            Assert.IsTrue(r.HasIssues);
            Assert.AreEqual(String.Empty, r.UsedCriteria);
            Assert.IsTrue(r.Count > 0); //All Patient resources should be found.
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
        public void Number_FindsResourceOnValidNumber()
        {
            var q = new Query().For("DocumentReference").Where("size=3654");
            var results = index.Search(q);
            Assert.AreEqual("size=3654", results.UsedCriteria);
            Assert.IsFalse(results.HasIssues);

            Assert.IsTrue(results.Count == 1);
            Assert.IsTrue(results.Has("DocumentReference/example"));
        }

        [TestMethod]
        public void Number_IgnoresAndReportsInvalidNumber()
        {
            var q = new Query().For("DocumentReference").Where("size=bla");
            var results = index.Search(q);
            Assert.AreEqual(String.Empty, results.UsedCriteria);
            Assert.IsTrue(results.HasIssues);

            //It should now have found all DocumentReferences, but there is only one in the examples.
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Has("DocumentReference/example"));
        }

        [TestMethod]
        public void Quantity()
        {
            SearchResults r;
            Query q = new Query().For("Encounter").AddParameter("length", "90||min");
            r = index.Search(q);
            Assert.IsTrue(r.Has("Encounter/f003"));
        }

        [TestMethod]
        public void Reference_FindsResourceOnReferenceId()
        {
            var q = new Query().For("Patient").Where("given=ned").Where("provider=Organization/hl7");
            var results = index.Search(q);
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
            var q = new Query().For("Condition").Where("subject.identifier=12345");
            var results = index.Search(q);
            //results = index.Search("Condition", "subject.identifier=12345");
            Assert.IsTrue(results.Count >= 2);
            Assert.IsTrue(results.Has("Condition/example"));
            Assert.IsTrue(results.Has("Condition/example2"));
        }

        [TestMethod]
        public void TripleChainToId_FindsResource()
        {
            var q = new Query().For("CarePlan").Where("condition.asserter.provider._id=Organization/1");
            var results = index.Search(q);

            Assert.AreEqual(1, results.Count);
            //TODO: Resulting query is OK, but there is no matching data in the examples. Find an example that does return a result.
        }

        [TestMethod]
        public void TripleChainToString_FindsResourceOnExactValue()
        {
            var q = new Query().For("CarePlan").Where("condition.asserter.provider.name=Gastroenterology");
            var results = index.Search(q);

            Assert.AreEqual(1, results.Count);
            //TODO: Resulting query is OK, but there is no matching data in the examples. Find an example that does return a result.
        }

        [TestMethod]
        public void Token_FindsResourceOnCodeOnly()
        {
            // No modifier
            var q = new Query().For("Patient").Where("gender=F");
            var results = index.Search(q);
            Assert.IsTrue(results.Has("Patient/80")); // Vera (woman)
        }

        [TestMethod]
        public void Token_FindsResourceOnText()
        {
            var q = new Query().For("Patient").Where("gender:text=male");
            var results = index.Search(q); // partial search op code, text or display. (includes "female"!)
            Assert.IsTrue(results.Has("Patient/80")); // Vera (woman)
        }

        [TestMethod]
        public void Token_FindsResourceOnNamespaceAndCode()
        {
            var q = new Query().For("Practitioner").Where("gender=urn:oid:2.16.840.1.113883.4.642.1.24|F");
            var results = index.Search(q);
            Assert.IsTrue(results.Has("Practitioner/f005"));
        }

        [TestMethod]
        public void Token_ExcludesResourceWhenNamespaceMatchesButCodeDoesNot()
        {
            var q = new Query().For("Practitioner").Where("gender=urn:oid:2.16.840.1.113883.4.642.1.24|M");
            var results = index.Search(q);
            Assert.IsFalse(results.Has("Practitioner/f005"));
        }

        [TestMethod]
        public void TokenText_FindsResourceOnEnglishText()
        {
            // Text modifier
            var q = new Query().For("Patient").Where("language:text=dutch");
            var results = index.Search(q);
            Assert.IsTrue(results.Has("Patient/f001"));
            Assert.IsTrue(results.Has("Patient/f201"));
        }

        [TestMethod]
        public void TokenText_DoesNotFindResourceOnAcronym()
        {
            // Text modifier
            var q = new Query().For("Patient").Where("language:text=nl");
            var results = index.Search(q);
            Assert.IsFalse(results.Has("Patient/f001"));
            Assert.IsFalse(results.Has("Patient/f201"));
        }

        [TestMethod]
        public void TokenText_FindsResourceOnDutchText()
        {
            // Text modifier
            var q = new Query().For("Patient").Where("language:text=Nederlands");
            var results = index.Search(q);
            Assert.IsTrue(results.Has("Patient/f001"));
            Assert.IsFalse(results.Has("Patient/f201"));
        }

        [TestMethod]
        public void Date_RejectsWrongFormat()
        {
            var q = new Query().For("Patient").Where("birthdate=19460608");
            var r = index.Search(q);
            Assert.IsTrue(r.HasIssues);
            Assert.AreEqual(String.Empty, r.UsedCriteria);
            Assert.IsTrue(r.Count > 0);//The only parameter is ignored, so the result contains all Patient resources.
        }

        [TestMethod]
        public void Date_FindsResourceOnPlainDate()
        {
            var q = new Query().For("Patient").Where("birthdate=1946-06-08");
            var r = index.Search(q);
            Assert.IsTrue(r.Has("Patient/106")); //James West
            Assert.IsTrue(r.Has("Patient/196")); //Marcus Hansen
            Assert.AreEqual(r.Count, 3); // James West is in the set twice, don't know why.
        }

        [TestMethod]
        public void Date_FindsResourceOnPlainDateAndString()
        {
            var q = new Query().For("Patient").Where("family=west").Where("birthdate=1946-06-08");
            var r = index.Search(q);
            Assert.IsTrue(r.Has("Patient/106")); // James West
            Assert.IsFalse(r.Has("Patient/196")); // Marcus Hansen is not in anymore
            Assert.AreEqual(r.Count, 2);
        }

        [TestMethod]
        public void Date_DoesNotFindResourceOnNonMatchingDate()
        {
            var q = new Query().For("Patient").Where("family=west").Where("birthdate=1946-06-07");
            var r = index.Search(q);
            Assert.AreEqual(r.Count, 0);
        }

        [TestMethod]
        public void Date_IgnoresAndReportsInvalidDate()
        {
            var q = new Query().For("Patient").Where("family=west").Where("birthdate=bla");
            var r = index.Search(q);

            Assert.IsTrue(r.HasIssues);
            Assert.AreEqual("family=west", r.UsedCriteria);
            Assert.AreEqual(2, r.Count); // Still all 'west' should be found
        }

        [TestMethod]
        public void DateLTE_DoesNotFindResourceOnNonMatchingDate()
        {
            var q = new Query().For("Patient").Where("family=west").Where("birthdate:before=<=1946-06-07");
            var r = index.Search(q);
            Assert.IsFalse(r.Has("Patient/106"));
        }

        [TestMethod]
        public void DateGTE_DoesNotFindResourceOnNonMatchingDateAndString()
        {
            var q = new Query().For("Patient").Where("family=west").Where("birthdate=>=1946-06-09");
            var r = index.Search(q);
            Assert.IsFalse(r.Has("Patient/106"));
        }

        [TestMethod]
        public void DateGTE_FindsResourceOnPlainDate()
        {
            var q = new Query().For("Patient").Where("family=west").Where("birthdate=>=1946-06-07");
            var r = index.Search(q);
            Assert.IsTrue(r.Has("Patient/106"));
        }

        [TestMethod]
        public void DateLTE_FindsResourceOnPlainDate()
        {
            var q = new Query().For("Patient").Where("family=west").Where("birthdate=<=1946-06-09");
            var r = index.Search(q);
            Assert.IsTrue(r.Has("Patient/106"));
        }

        //Next 5 tests: CarePlan/preg has date.start = 2013-01-01, date.end = 2013-10-01
        [TestMethod]
        public void DatePeriod_FindsResourceOnPlainDate()
        {
            var q = new Query().For("CarePlan").Where("date=2013-01-01");
            var r = index.Search(q);
            Assert.IsTrue(r.Has("CarePlan/preg"));
        }

        [TestMethod]
        public void DatePeriodGTE_FindsResourceOnPlainDate()
        {
            var q = new Query().For("CarePlan").Where("date=>=2013-01-01");
            var r = index.Search(q);
            Assert.IsTrue(r.Has("CarePlan/preg")); // should still be included because of overlap.
        }

        [TestMethod]
        public void DatePeriodLTE_FindsResourceOnPlainDate()
        {
            var q = new Query().For("CarePlan").Where("date=<=2013-10-01");
            var r = index.Search(q);
            Assert.IsTrue(r.Has("CarePlan/preg")); // should still be included because of overlap.
        }

        [TestMethod]
        public void DatePeriodGT_FindsResourceOnPlainDate()
        {
            var q = new Query().For("CarePlan").Where("date=>2013-01-01");
            var r = index.Search(q);
            Assert.IsFalse(r.Has("CarePlan/preg"));
        }

        [TestMethod]
        public void DatePeriodLT_FindsResourceOnPlainDate()
        {
            var q = new Query().For("CarePlan").Where("date=<2013-10-01");
            var r = index.Search(q);
            Assert.IsFalse(r.Has("CarePlan/preg"));
        }

        [TestMethod]
        public void Reference_FindsResourceOnValidId()
        {
            var q = new Query().For("Patient").Where("given=ned").Where("provider=Organization/hl7");
            var results = index.Search(q);
            Assert.AreEqual(1, results.Count);
        }

        [TestMethod]
        public void Reference_FindsResourceOnFullUri()
        {
            //TODO: This tests shows an error reported by David Hay, we should fix it by making the URI relative before searching.
            var q = new Query().For("Questionnaire").Where("subject=http://spark.furore.com/fhir/Patient/f201");
            var results = index.Search(q);
            Assert.AreEqual(1, results.Count);
        }

        [TestMethod]
        public void Reference_DoesNotFindResourceOnInvalidId()
        {
            var q = new Query().For("Patient").Where("given=ned").Where("provider=nonExistingOrganization");
            var results = index.Search(q);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Composite_FindsResource()
        {
            var q = new Query().For("DiagnosticOrder").Where("event-status-date=Requested$2013-05-02");
            var results = index.Search(q);
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Has("DiagnosticOrder/example"));
        }

        [TestMethod]
        public void CompositeChoice_FindsResource()
        {
            var q = new Query().For("DiagnosticOrder").Where("event-status-date=Requested$2013-05-02,Bla$2012-04-05");
            var results = index.Search(q);
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Has("DiagnosticOrder/example"));
        }

        [TestMethod]
        public void Tag_FindsResourceOnExactString()
        {
            var results = index.Search(new Query().For("Patient").Where(String.Format("_tag={0}", _otherTag)));
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Has("Patient/tagged"));
        }

        [TestMethod]
        public void Tag_DoesNotFindResourceOnFirstPartOfString()
        {
            var results = index.Search(new Query().For("Patient").Where(String.Format("_tag={0}", _otherTag.Substring(0, 4))));
            Assert.IsFalse(results.Has("Patient/tagged"));
        }

        [TestMethod]
        public void TagPartial_FindsResourceOnExactString()
        {
            var results = index.Search(new Query().For("Patient").Where(String.Format("_tag:partial={0}", _otherTag)));
            Assert.IsTrue(results.Has("Patient/tagged"));
        }

        [TestMethod]
        public void TagPartial_FindsResourceOnFirstPartOfString()
        {
            var results = index.Search(new Query().For("Patient").Where(String.Format("_tag:partial={0}", _otherTag.Substring(0, 4))));
            Assert.IsTrue(results.Has("Patient/tagged"));
        }

        [TestMethod]
        public void TagText_DoesNotFindResourceOnExactString()
        {
            var results = index.Search(new Query().For("Patient").Where(String.Format("_tag:text={0}", _otherTag)));
            Assert.IsFalse(results.Has("Patient/tagged"));
        }

        [TestMethod]
        public void TagText_FindsResourceOnFullText()
        {
            var results = index.Search(new Query().For("Patient").Where("_tag:text=dummy"));
            Assert.IsTrue(results.Has("Patient/tagged"));
        }

        [TestMethod]
        public void Count_LimitsToSpecifiedNumberOfResults()
        {
            var q = new Query().For("Patient").LimitTo(3);
            var results = index.Search(q);

            Assert.AreEqual(3, results.Count);
        }
    }
}
