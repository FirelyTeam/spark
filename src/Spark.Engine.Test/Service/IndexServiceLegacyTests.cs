using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using System.Collections.Generic;
using Spark.Engine.Search;
using Spark.Engine.Model;
using Spark.Search;
using static Hl7.Fhir.Model.ModelInfo;
using Moq;
using Hl7.Fhir.Serialization;
using System.Linq;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using System.IO;

namespace Spark.Engine.Test.Service
{
    [TestClass]
    public class IndexServiceLegacyTests
    {
        private IndexServiceLegacy sutLimited;
        private IndexServiceLegacy sutFull;
        private string _examplePatientJson;
        private string _exampleAppointmentJson;
        private string _carePlanWithContainedGoal;
        private string _exampleObservationJson;

        [TestInitialize]
        public void TestInitialize()
        {
            IFhirModel _fhirModel;
            FhirPropertyIndex _propIndex;
            ResourceVisitor _resourceVisitor;
            ElementIndexer _elementIndexer;
            var _indexStoreMock = new Mock<IIndexStore>();
            _examplePatientJson = TextFileHelper.ReadTextFileFromDisk($".{Path.DirectorySeparatorChar}Examples{Path.DirectorySeparatorChar}patient-example.json");
            _exampleAppointmentJson = TextFileHelper.ReadTextFileFromDisk($".{Path.DirectorySeparatorChar}Examples{Path.DirectorySeparatorChar}appointment-example2doctors.json");
            _carePlanWithContainedGoal = TextFileHelper.ReadTextFileFromDisk($".{Path.DirectorySeparatorChar}Examples{Path.DirectorySeparatorChar}careplan-example-f201-renal.json");
            _exampleObservationJson = TextFileHelper.ReadTextFileFromDisk($".{Path.DirectorySeparatorChar}Examples{Path.DirectorySeparatorChar}observation-example-bloodpressure.json");
            var spPatientName = new SearchParamDefinition() { Resource = "Patient", Name = "name", Description = new Markdown(@"A portion of either family or given name of the patient"), Type = SearchParamType.String, Path = new string[] { "Patient.name", } };
            var searchParameters = new List<SearchParamDefinition> { spPatientName };
            var resources = new Dictionary<Type, string> { { typeof(Patient), "Patient" }, { typeof(HumanName), "HumanName" } };
            //CK: I use real objects: saves me a lot of mocking and provides for a bit of integration testing.
            _fhirModel = new FhirModel(resources, searchParameters);
            _propIndex = new FhirPropertyIndex(_fhirModel, new List<Type> { typeof(Patient), typeof(HumanName) });
            _resourceVisitor = new ResourceVisitor(_propIndex);
            _elementIndexer = new ElementIndexer(_fhirModel);

            //_indexStoreMock.Setup(ixs => ixs.Save(It.IsAny<IndexValue>))

            sutLimited = new IndexServiceLegacy(_fhirModel, _propIndex, _resourceVisitor, _elementIndexer, _indexStoreMock.Object);

            _fhirModel = new FhirModel(); //For this test I want all available types and searchparameters.
            _propIndex = new FhirPropertyIndex(_fhirModel);
            _resourceVisitor = new ResourceVisitor(_propIndex);
            _elementIndexer = new ElementIndexer(_fhirModel);

            sutFull = new IndexServiceLegacy(_fhirModel, _propIndex, _resourceVisitor, _elementIndexer, _indexStoreMock.Object);
        }

        [TestMethod]
        public void TestIndexResourceSimple()
        {
            var patient = new Patient();
            patient.Name.Add(new HumanName().WithGiven("Adriaan").AndFamily("Bestevaer"));

            IKey patientKey = new Key("http://localhost/", "Patient", "001", "v02");

            IndexValue result = sutLimited.IndexResource(patient, patientKey);

            Assert.AreEqual("root", result.Name);
            Assert.AreEqual(1, result.NonInternalValues().Count(), "Expected 1 non-internal result for searchparameter 'name'");
            var first = result.NonInternalValues().First();
            Assert.AreEqual("name", first.Name);
            Assert.AreEqual(2, first.Values.Count);
            Assert.IsInstanceOfType(first.Values[0], typeof(StringValue));
            Assert.IsInstanceOfType(first.Values[1], typeof(StringValue));
        }

        [TestMethod]
        public void TestIndexResourcePatientComplete()
        {
            FhirJsonParser parser = new FhirJsonParser();
            var patientResource = parser.Parse<Resource>(_examplePatientJson);

            IKey patientKey = new Key("http://localhost/", "Patient", "001", null);

            IndexValue result = sutFull.IndexResource(patientResource, patientKey);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestIndexResourceAppointmentComplete()
        {
            FhirJsonParser parser = new FhirJsonParser();
            var appResource = parser.Parse<Resource>(_exampleAppointmentJson);

            IKey appKey = new Key("http://localhost/", "Appointment", "2docs", null);

            IndexValue result = sutFull.IndexResource(appResource, appKey);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestIndexResourceCareplanWithContainedGoal()
        {
            FhirJsonParser parser = new FhirJsonParser();
            var cpResource = parser.Parse<Resource>(_carePlanWithContainedGoal);

            IKey cpKey = new Key("http://localhost/", "Careplan", "f002", null);

            IndexValue result = sutFull.IndexResource(cpResource, cpKey);

            Assert.IsNotNull(result);
        }


        [TestMethod]
        public void TestIndexResourceObservation()
        {
            FhirJsonParser parser = new FhirJsonParser();
            var obsResource = parser.Parse<Resource>(_exampleObservationJson);

            IKey cpKey = new Key("http://localhost/", "Observation", "blood-pressure", null);

            IndexValue result = sutFull.IndexResource(obsResource, cpKey);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestIndexWithPath_x_()
        {
            Condition cd = new Condition();
            cd.Onset = new FhirDateTime(2015, 6, 15);

            IKey cdKey = new Key("http://localhost/", "Condition", "test", null);

            IndexValue result = sutFull.IndexResource(cd, cdKey);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Values.Where(iv => (iv as IndexValue).Name == "onset"));
        }
    }

    public static class IndexValueTestExtensions
    {
        public static IEnumerable<IndexValue> NonInternalValues(this IndexValue root)
        {
            return root.IndexValues().Where(v => !v.Name.StartsWith("internal_"));
        }
    }
}
