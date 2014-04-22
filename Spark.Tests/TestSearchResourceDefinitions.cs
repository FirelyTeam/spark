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

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Hl7.Fhir.Support;
using Spark.Support;
using Spark.Store;
using Spark.Search;
using Hl7.Fhir.Model;
using Spark.Service;
using Spark.Core;
using System.Configuration;
using Spark.Config;

namespace SparkTests.Search
{
    [TestClass]
    public class TestSearchResourceDefinitions
    {
        static SearchResults results;
        static IFhirIndex index = null;

        [ClassInitialize]
        public static void Import(TestContext unused)
        {
            Dependencies.Register();
            Settings.AppSettings = ConfigurationManager.AppSettings;
            FhirMaintainanceService maintainance = Factory.GetFhirMaintainceService();

            index = Factory.GetIndex();
        }

        [TestMethod]
        public void Patient()
        {
            //id
            
            results = index.Search("Patient", "_id=xds");
            Assert.IsTrue(results.Has("Patient/xds"));

            // Adres
            results = index.Search("Patient", "address=44130");
            Assert.IsTrue(results.Has("Patient/xds"));

            results = index.Search("Patient", "address=44130&address=usa");
            Assert.IsTrue(results.Has("Patient/xds"));

            results = index.Search("Patient", "address=44130&address=fr");
            Assert.IsFalse(results.Has("Patient/xds"));

            // Animal-breed
            results = index.Search("Patient", "animal-breed=58108001");
            Assert.IsTrue(results.Has("Patient/animal"));

            results = index.Search("Patient", "animal-breed=/58108001");
            Assert.IsFalse(results.Has("Patient/animal"));

            // Birthdate 
            results = index.Search("Patient", "birthdate=19911225");
            Assert.IsTrue(results.Has("http://hl7.org/fhir/Patient/77"));

            results = index.Search("Patient", "birthdate:before=1992");
            Assert.IsTrue(results.Has("http://hl7.org/fhir/Patient/77"));

            results = index.Search("Patient", "birthdate:before=1991");
            Assert.IsFalse(results.Has("http://hl7.org/fhir/Patient/77"));

            results = index.Search("Patient", "birthdate:after=1991");
            Assert.IsTrue(results.Has("http://hl7.org/fhir/Patient/77"));

            // Gender
            results = index.Search("Patient", "gender=M");
            Assert.IsTrue(results.Has("Patient/example"));

            //results = Factory.TestIndex.Search("patient", "identifier=...");
            // no testable data.

            // Language
            results = index.Search("Patient", "language=");
            Assert.IsTrue(results.Has("http://hl7.org/fhir/Patient/77"));

            // Telecom
            results = index.Search("Patient", "telecom=555-555-2007");
            Assert.IsTrue(results.Has("http://hl7.org/fhir/Patient/9"));

            // Phonetic
            results = index.Search("Patient", "phonetic=chalmurs");
            Assert.IsTrue(results.Has("Patient/example"));
        }

        [TestMethod]
        public void Practitioner()
        {
            results = index.Search("Practitioner", "address=healthcare");
            Assert.IsTrue(results.Has("Practitioner/13"));

            results = index.Search("Practitioner", "address=1002 healthcare");
            Assert.IsTrue(results.Has("Practitioner/13"));

            // Als address een text parameter was had deze ook gevonden moeten worden, maar het is een string-search:
            results = index.Search("Practitioner", "address=healthcare 1002"); 
            Assert.IsFalse(results.Has("Practitioner/13"));
            
            results = index.Search("Practitioner", "family=seven");
            Assert.IsTrue(results.Has("Practitioner/13"));

            
            /*
            Volgens de standaard, moet deze falen, maar volgens de voorbeelden in de standaard niet !!?

            results = index.Search("practitioner", "gender=http://hl7.org/fhir/v3/AdministrativeGender|M");
            Assert.IsTrue(results.Has("practitioner/13"));
            */

            results = index.Search("Practitioner", "given=henry");
            Assert.IsTrue(results.Has("Practitioner/13"));

            // language - no data

            results = index.Search("Practitioner", "phonetic=bleeder"); //Bleeder
            Assert.IsTrue(results.Has("Practitioner/35"));

            results = index.Search("Practitioner", "phonetic=blater"); //Bleeder
            Assert.IsTrue(results.Has("Practitioner/35"));

            results = index.Search("Practitioner", "telecom=555-555-1035");
            Assert.IsTrue(results.Has("Practitioner/35"));
        }
            

      
    }
}
