/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Hl7.Fhir.Support;
using Spark.Support;
using Spark.Store;
using Spark.Core;
using Spark.Search;
using Hl7.Fhir.Model;
using Spark.Service;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Hl7.Fhir.Rest;
using Spark.Config;
using Hl7.Fhir.Search;

namespace SparkTests.Search
{
    [TestClass]
    public partial class TestSearchConformance
    {
        private static FhirIndex index;

        [ClassInitialize]
        public static void Import(TestContext unused) 
        {
            Dependencies.Register();
            Settings.AppSettings = ConfigurationManager.AppSettings;

            //FhirMaintenanceService maintenance = Factory.GetFhirMaintenceService();
            //maintenance.Initialize();

            index = Factory.GetIndex();
            
        }

        [TestInitialize]
        public void Start()
        {
             
        }

        [TestMethod]
        public void DefaultSearch()
        {
            SearchResults r;
            r = index.Search("Patient", "name=\"Doe\"");
            Assert.IsTrue(r.Has("Patient/xds"));

            r = index.Search("Patient", "name=\"Doetje\"");
            Assert.IsFalse(r.Has("Patient/xds"));

            r = index.Search("Patient", "name=\"doe\"");
            Assert.IsTrue(r.Has("Patient/xds"));

            r = index.Search("Patient", "name=\"Doe\",\"John\"");
            Assert.IsTrue(r.Has("Patient/xds"));

            r = index.Search("Patient", "name=Doe&name=\"John\"");
            Assert.IsTrue(r.Has("Patient/xds"));

            r = index.Search("Patient", "name=\"Doe\", \"John\"");
            Assert.IsTrue(r.Has("Patient/xds"));
        }

        [TestMethod]
        public void UnknownResource()
        {
            //Nonexisting resource should give an exception. But the server will catch it before it reaches search.

            SearchResults r;
            r = index.Search("Pratient", "name=Doe&name=\"John\"");
            Assert.IsFalse(r.Has("Patient/xds"));

        }
        [TestMethod]
        public void MaxResults()
        {
            // Non standard feature!
            SearchResults r;
            r = index.Search("Patient", "_limit=10");
            Assert.IsTrue(r.Count() == 10);

        }

        [TestMethod]
        public void History()
        {
            SearchResults r;
            r = index.Search("Patient", "name=\"Doe\"");
            Assert.IsTrue(r.Has("Patient/xds"));

            r = index.Search("Patient", "name=\"Doe\"");
            Assert.IsTrue(r.Has("Patient/xds/_history/504"));

            r = index.Search("Patient", "name=\"Doetje\"");
            Assert.IsFalse(r.Has("Patient/xds/_history/504"));
        }

       
        [TestMethod]
        public void ModifierMissing()
        {
            SearchResults r;
            r = index.Search("Patient", "provider:missing=true&family=\"brooks\"");
            Assert.IsTrue(r.Has("Patient/ihe-pcd"));

            r = index.Search("Patient", "provider:missing=false&family=\"brooks\"");
            Assert.IsFalse(r.Has("Patient/ihe-pcd"));
        }

        
        [TestMethod]
        public void TokenParameters()
        {
            SearchResults results;

            // No modifier
            results = index.Search("Patient", "gender=F"); // partial search op code, text or display. (includes code=F en display="Female")
            Assert.IsTrue(results.Has("Patient/80")); // Vera (woman)

            results = index.Search("Patient", "gender=male"); // partial search op code, text or display. (includes "female"!)
            Assert.IsTrue(results.Has("Patient/80")); // Vera (woman)

            /*
            This fails. The DSTU spec says that it should fail. but the examples say that they should succeed.
            
            Spec:
                "Without modifier, the search will use the textual parameter to do a partial match on code, text or display."
            
            Example:
                GET [base-url]/patient?identifier=http://acme.org/patient/2345
                "Search for all the patients with an identifier with key = "2345" in the system "http://acme.org/patient""
            */

            results = index.Search("Practitioner", "gender=http://hl7.org/fhir/v3/AdministrativeGender|F");
            Assert.IsTrue(results.Has("Practitioner/f005"));

            results = index.Search("Practitioner", "gender=http://hl7.org/fhir/v3/AdministrativeGender|M");
            Assert.IsFalse(results.Has("Practitioner/f005"));


            // Text modifier
            results = index.Search("Patient", "language:text=dutch");
            Assert.IsTrue(results.Has("Patient/f001"));
            Assert.IsTrue(results.Has("Patient/f201"));

            results = index.Search("Patient", "language:text=nl");
            Assert.IsFalse(results.Has("Patient/f001"));
            Assert.IsFalse(results.Has("Patient/f201"));

            results = index.Search("Patient", "language:text=Nederlands");
            Assert.IsTrue(results.Has("Patient/f001"));
            Assert.IsFalse(results.Has("Patient/f201"));


            // Code modifier
            results = index.Search("Patient", "gender:code=http://hl7.org/fhir/v3/AdministrativeGender|M");
            Assert.IsTrue(results.Count() > 0);

            results = index.Search("Patient", "gender:code=http://snomed.info/id|248153007");
            Assert.IsTrue(results.Count() > 0);

            results = index.Search("Patient", "gender:code=http://snomed.info/id|M"); // !! patients with both hl7 and snomed code should not be found when mixed
            Assert.IsTrue(results.Count() == 0);

            // AnyNs modifier
            results = index.Search("Patient", "gender:anyns=F"); // partial search op code, text or display. (includes "female"!)
            Assert.IsTrue(results.Has("Patient/80")); // Vera (woman)
        }

        [TestMethod]
        public void TokenSplitter()
        {
            SearchResults results;
            results = index.Search("Patient", "gender:code=http://hl7.org/fhir/v3/AdministrativeGender|M");
            Assert.IsTrue(results.Count() > 0);
        }

        [TestMethod]
        public void StringParameters()
        {
            SearchResults r; // Ross Mckinney

            // Default search
            r = index.Search("Patient", "family=\"Mckinney\"");
            Assert.IsTrue(r.Has("Patient/76"));

            r = index.Search("Patient", "family=\"Mckinley\"");
            Assert.IsFalse(r.Has("Patient/76"));

            r = index.Search("Patient", "family=\"McKinney\"");
            Assert.IsTrue(r.Has("Patient/76"));

            r = index.Search("Patient", "family=\"kinney\"");
            Assert.IsTrue(r.Has("Patient/76"));

            r = index.Search("Patient", "family=\"Mckinn\"");
            Assert.IsTrue(r.Has("Patient/76"));

            r = index.Search("Patient", "family=\"kinne\"");
            Assert.IsTrue(r.Has("Patient/76"));

            // Exact search
            r = index.Search("Patient", "family:exact=\"Mckinney\"");
            Assert.IsTrue(r.Has("Patient/76"));

            r = index.Search("Patient", "family:exact=\"Mckinley\"");
            Assert.IsFalse(r.Has("Patient/76"));

            r = index.Search("Patient", "family:exact=\"McKinney\"");
            Assert.IsFalse(r.Has("Patient/76"));

            r = index.Search("Patient", "family:exact=\"kinney\"");
            Assert.IsFalse(r.Has("Patient/76"));

            r = index.Search("Patient", "family:exact=\"Mckinn\"");
            Assert.IsFalse(r.Has("Patient/76"));

            r = index.Search("Patient", "family:exact=\"kinne\"");
            Assert.IsFalse(r.Has("Patient/76"));

            // Partial search
            r = index.Search("Patient", "family:partial=\"Mckinney\"");
            Assert.IsTrue(r.Has("Patient/76"));

            r = index.Search("Patient", "family:partial=\"Mckinley\"");
            Assert.IsFalse(r.Has("Patient/76"));

            r = index.Search("Patient", "family:partial=\"McKinney\""); //ci
            Assert.IsTrue(r.Has("Patient/76"));

            r = index.Search("Patient", "family:partial=\"kinney\"");
            Assert.IsFalse(r.Has("Patient/76"));

            r = index.Search("Patient", "family:partial=\"Mckinn\"");
            Assert.IsTrue(r.Has("Patient/76"));

            r = index.Search("Patient", "family:partial=\"kinne\"");
            Assert.IsFalse(r.Has("Patient/76"));
        }

        [TestMethod]
        public void DateAndPeriod()
        {
            SearchResults r; 

            // Date 
            r = index.Search("Patient", "birthdate=19460608");
            Assert.IsTrue(r.Has("Patient/106")); 
            Assert.IsTrue(r.Has("Patient/196"));
            Assert.AreEqual(r.Count(),  3); // James West, Marcus Hansen

            // NB. This test fails, because patient James (Tiberius) West exists in the examples twice now. Not clear if this is intentional 

            r = index.Search("Patient", "family=west&birthdate=19460608");
            Assert.IsTrue(r.Has("Patient/106"));
            Assert.IsFalse(r.Has("Patient/196"));
            Assert.AreEqual(r.Count(), 2); // James West

            r = index.Search("Patient", "family=west&birthdate=19460607");
            Assert.AreEqual(r.Count(), 0);

            r = index.Search("Patient", "family=west&birthdate:before=19460607");
            Assert.IsFalse(r.Has("Patient/106"));

            r = index.Search("Patient", "family=west&birthdate:after=19460609");
            Assert.IsFalse(r.Has("Patient/106"));

            r = index.Search("Patient", "family=west&birthdate:after=19460607");
            Assert.IsTrue(r.Has("Patient/106"));

            r = index.Search("Patient", "family=west&birthdate:before=19460609");
            Assert.IsTrue(r.Has("Patient/106"));

            // Period 
            r = index.Search("CarePlan", "date=20130101");
            Assert.IsTrue(r.Has("CarePlan/preg"));
            
            r = index.Search("CarePlan", "date:after=20130101");
            Assert.IsTrue(r.Has("CarePlan/preg")); // shlould still include because of overlap.

            r = index.Search("CarePlan", "date:before=20131001");
            Assert.IsTrue(r.Has("CarePlan/preg"));

            r = index.Search("CarePlan", "date:after=20131002");
            Assert.IsFalse(r.Has("CarePlan/preg")); // shlould still include because of overlap.

            r = index.Search("CarePlan", "date:before=20130931");
            Assert.IsFalse(r.Has("CarePlan/preg"));

        }

        [TestMethod]
        public void ReferenceParameters()
        {
            SearchResults results;
            results = index.Search("Patient", "given=ned&provider=Organization/hl7 ");
            Assert.IsTrue(results.Count == 1);
            
            results = index.Search("Patient", "given=ned&provider=Organization/hl7 ");
            Assert.IsTrue(results.Count == 1);
        }

        [TestMethod]
        public void ChainedParameter()
        {
            SearchResults results;

            results = index.Search("Patient", "given=\"ned\"&provider=Organization/hl7 ");
            Assert.IsTrue(results.Count == 1);

            results = index.Search("Patient", "given=\"nancy\"&family=\"nuclear\"");
            Assert.IsTrue(results.Count == 1);

            results = index.Search("Organization", "name:partial=\"health\"");
            Assert.IsTrue(results.Count == 1);

            results = index.Search("Patient", "given=\"nancy\"&provider:Organization.name:partial=\"health\"");
            Assert.IsTrue(results.Count == 1);

            results = index.Search("Patient", "given=\"ned\"&provider:Organization.name:partial=\"healthy\"");
            Assert.IsTrue(results.Count == 0);

            results = index.Search("Patient", "given=\"ned\"&provider:Organization.name:exact=\"Health Level Seven International\"");
            Assert.IsTrue(results.Count == 1);

            results = index.Search("Patient", "given=\"ned\"&provider:Organization.name:exact=\"Health Level seven International\"");
            Assert.IsTrue(results.Count == 0);

            results = index.Search("Patient", "given=\"ned\"&provider:Organization.name:exact=\"Health Level Seven Inter\"");
            Assert.IsTrue(results.Count == 0);

            results = index.Search("Patient", "given=\"ned\"&provider:Organization.name:partial=Health Level Seven Inter");
            Assert.IsTrue(results.Count == 1);

            results = index.Search("Patient", "given=\"nancy\"&provider:Organization.name:partial=health,healthy");
            Assert.IsTrue(results.Count == 1);

            results = index.Search("Patient", "given=\"nancy\"&provider:Organization.name:partial=healthy,healthy");
            Assert.IsTrue(results.Count == 0);
            
            results = index.Search("Patient", "given=\"nancy\"&provider:Organization.name:partial=healthy,healthy,health");
            Assert.IsTrue(results.Count == 1);
            
            // THE  WORKS
            results = index.Search("Condition", "subject.identifier=12345");
            Assert.IsTrue(results.Has("Condition/example"));
            Assert.IsTrue(results.Has("Condition/example2"));
            Assert.IsTrue(results.Count > 2);

            results = index.Search("Condition", "subject.identifier=12345");
            Assert.IsTrue(results.Has("Condition/example"));
            Assert.IsTrue(results.Has("Condition/example2"));
            Assert.IsTrue(results.Count > 2);
            
            


            // deeper chains:
            //results = Factory.TestIndex.Search("CarePlan", "condition.patient.provider._id=organization/1");
            // CarePlan.Condition -> Problem
            // Problem.Subject -> Patient
            // Patient.Provider -> Organization 
            // Organization._id -> organization/1 (liever had ik .name gehad, maar /1 bestaat niet.
            // Assert.IsTrue(results.Count == 1);
        }

        [TestMethod]
        public void CombinedParameters()
        {
            // OR construct
            SearchResults results;

            results = index.Search("Patient", "name=\"christine\",\"rivera\""); // both match
            Assert.IsTrue(results.Has("Patient/82"));

            results = index.Search("Patient", "name=\"pipo\",\"rivera\""); // 1st of 2 match
            Assert.IsTrue(results.Has("Patient/82"));
            
            results = index.Search("Patient", "name=\"christine\",\"pipo\""); // 2nd of 2 match
            Assert.IsTrue(results.Has("Patient/82"));

            results = index.Search("Patient", "name=\"pipo\",\"clown\""); // neither match 
            Assert.IsFalse(results.Has("Patient/82"));

            results = index.Search("Patient", "name:partial=\"chris\",\"rive\""); // both match partial
            Assert.IsTrue(results.Has("Patient/82"));
        }

        [TestMethod]
        public void ContainedResources()
        {
            //Test on finding a contained resource.
            var results = index.Search("CarePlan", "participant.family=Mavis"); // sic. "dietician' exists as a family name in the examples data
            Assert.IsTrue(results.Has("CarePlan/preg"));
        }

        [TestMethod]
        public void Includes()
        {
            SearchResults results = index.Search("Patient", "family=nuclear&_include=Patient.provider");
            Assert.IsTrue(results.Has("Patient/4"));
            Assert.IsTrue(results.Has("Patient/5"));
            Assert.IsTrue(results.Has("Patient/6"));
            Assert.IsTrue(results.Has("Patient/7"));
            Assert.IsTrue(results.Has("Organization/hl7"));            
            Assert.IsTrue(results.Count == 5);
        }

        [TestMethod]
        public void UsedParameters()
        {

            // TEST: Skip empty parameters
            Parameters parameters;
            SearchResults results;

            // Skip empty parameter
            parameters = ParameterFactory.Parameters(Factory.Definitions, "Patient", "family=\"Doe\"&family=&given=\"John\"");
            results = index.Search(parameters);
            Assert.AreEqual(parameters.WhichFilter.Count(), 3); // meta_category=patient, family=Doe, given=John
            Assert.AreEqual(parameters.Used.Count(), 2);    // meta_category=patient, family=Doe, given=John
            Assert.IsTrue(results.Has("Patient/xds"));

            parameters = ParameterFactory.Parameters(Factory.Definitions, "Patient", "family=\"Doe\"&family=\"\"&given=\"John\"");
            results = index.Search(parameters);
            Assert.AreEqual(parameters.WhichFilter.Count(), 4); // meta_category=patient, family=Doe, family="", given=John
            Assert.AreEqual(parameters.Used.Count(), 3);    // meta_category=patient, family=Doe, given=John
            Assert.IsTrue(results.Has("Patient/xds")); // empty string search is full (not partial) - thus: search succeedes here.

            // Skip nonexistent parameter
            parameters = ParameterFactory.Parameters(Factory.Definitions, "Patient", "name=\"Doe\"&tree=oak&given=\"John\"");
            results = index.Search("Patient", "family=\"Doe\"&given=&given=\"John\"");
            Assert.AreEqual(parameters.WhichFilter.Count(), 3); // meta_category=patient, family=Doe, given=John
            Assert.AreEqual(parameters.Used.Count(), 2);    // family=Doe, given=John
            Assert.IsTrue(results.Has("Patient/xds"));

            // Used http query
            parameters = ParameterFactory.Parameters(Factory.Definitions, "Patient", "name=\"Donald\"&family=\"Duck\"&name=\"John\"&name=&pipo=\"clown\"");
            string s = parameters.UsedHttpQuery();
            //Assert.AreEqual(s, "name=\"Donald\"&name=\"John\"&family=\"Duck\"");
            Assert.AreEqual(s, "name=\"Donald\"&family=\"Duck\"&name=\"John\"");

            // parameter without a field 
            parameters = ParameterFactory.Parameters(Factory.Definitions, "Patient", "hello");
            Assert.AreEqual(parameters.WhichFilter.Count(), 1); // meta_category=patient 
            Assert.AreEqual(parameters.Used.Count(), 0);    // 

            // No parameters
            parameters = ParameterFactory.Parameters(Factory.Definitions, "Patient", "");
            Assert.AreEqual(parameters.WhichFilter.Count(), 1); // meta_category=patient 
            Assert.AreEqual(parameters.Used.Count(), 0);    // 
            results = index.Search(parameters);
            Assert.IsTrue(results.Count > 50);

            // global field _id
            parameters = ParameterFactory.Parameters(Factory.Definitions, "Patient", "_id=Patient/100");
            s = parameters.UsedHttpQuery();
            Assert.AreEqual(s, "_id=Patient/100");
        }

        [TestMethod]
        public void UniversalFields()
        {
            SearchResults results;

            results = index.Search("Patient", "_id=example"); 
            Assert.IsTrue(results.Has("Patient/example"));

            results = index.Search("Condition", "subject:Patient._id=example");
            Assert.IsTrue(results.Has("Condition/example"));
            Assert.IsTrue(results.Has("Condition/example2"));

            results = index.Search("Condition", "subject._id=example");
            Assert.IsTrue(results.Has("Condition/example"));
            Assert.IsTrue(results.Has("Condition/example2"));
        }

        [TestMethod]
        public void SoundexTest()
        {
            string s1 = Soundex.For("Jansen");
            string s2 = Soundex.For("Jenssen");
            Assert.AreEqual(s1, s2);
        }


        [TestMethod]
        public void ResourceIdentityComparing()
        {
            ResourceIdentity a, b;

            // Testing the testing method

            a = new ResourceIdentity("Patient/1");
            b = new ResourceIdentity("Patient/1");
            Assert.IsTrue(a.SameAs(b));

            a = new ResourceIdentity("Patient/1");
            b = new ResourceIdentity("Patient/2");
            Assert.IsFalse(a.SameAs(b));

            a = new ResourceIdentity("Patient/1/_history/1");
            b = new ResourceIdentity("Patient/1");
            Assert.IsTrue(a.SameAs(b));

            a = new ResourceIdentity("Patient/1/_history/1");
            b = new ResourceIdentity("Patient/2");
            Assert.IsFalse(a.SameAs(b));

            a = new ResourceIdentity("Patient/1/_history/1");
            b = new ResourceIdentity("Patient/1/_history/2");
            Assert.IsFalse(a.SameAs(b));

            a = new ResourceIdentity("Patient/1/_history/1");
            b = new ResourceIdentity("Patient/2/_history/1");
            Assert.IsFalse(a.SameAs(b));
        }

        [TestMethod]
        public void Tags()
        {
            
            SearchResults results;

            results = index.Search("Patient", "_tag=http://readtag.hl7.nl");
            Assert.IsTrue(results.Has("Patient/10135"));

            results = index.Search("Patient", "_tag=exam");
            Assert.IsFalse(results.Has("Patient/example"));

            results = index.Search("Patient", "_tag:partial=example");
            Assert.IsTrue(results.Has("Patient/example"));

            results = index.Search("Patient", "_tag:partial=exam");
            Assert.IsTrue(results.Has("Patient/example"));

            results = index.Search("Patient", "_tag:text=example");
            Assert.IsFalse(results.Has("Patient/example"));

            results = index.Search("Patient", "_tag:text=labelloexample");
            Assert.IsTrue(results.Has("Patient/example"));
        }

        
    }
}
