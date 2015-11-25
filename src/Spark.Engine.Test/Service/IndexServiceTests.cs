using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hl7.Fhir.Model;
using Spark.Engine.Service;
using Spark.Engine.Core;
using System.Collections.Generic;
using Spark.Engine.Search;
using Spark.Engine.Model;
using Spark.Search;
using static Hl7.Fhir.Model.ModelInfo;

namespace Spark.Engine.Test.Service
{
    [TestClass]
    public class IndexServiceTests
    {

        private IFhirModel _fhirModel;
        private FhirPropertyIndex _propIndex;
        private ResourceVisitor _resourceVisitor;
        private ElementIndexer _elementIndexer;

        private IndexService sut;

        [TestInitialize]
        public void TestInitialize()
        {
            var spPatientName = new SearchParamDefinition() { Resource = "Patient", Name = "name", Description = @"A portion of either family or given name of the patient", Type = SearchParamType.String, Path = new string[] { "Patient.name", } };
            var searchParameters = new List<SearchParamDefinition> { spPatientName };
            var resources = new Dictionary<Type, string> { { typeof(Patient), "Patient" }, { typeof(HumanName), "HumanName" } };
            var enums = new List<Type>();
            //CK: I use real objects: saves me a lot of mocking and provides for a bit of integration testing.
            _fhirModel = new FhirModel(resources, searchParameters, enums);
            _propIndex = new FhirPropertyIndex(_fhirModel, new List<Type> { typeof(Patient), typeof(HumanName) });
            _resourceVisitor = new ResourceVisitor(_propIndex);
            _elementIndexer = new ElementIndexer(_fhirModel);

            sut = new IndexService(_fhirModel, _propIndex, _resourceVisitor, _elementIndexer);
        }

        [TestMethod]
        public void TestIndexResourceSimple()
        {
            var patient = new Patient();
            patient.Name.Add(new HumanName().WithGiven("Adriaan").AndFamily("Bestevaer"));

            IKey patientKey = new Key("http://localhost/", "Patient", "001", "v02");

            IndexValue result = sut.IndexResource(patient, patientKey);

            Assert.AreEqual("root", result.Name);
            Assert.AreEqual(5, result.Values.Count, "Expected 1 result for searchparameter 'name' and 4 for meta info");
            Assert.IsInstanceOfType(result.Values[0], typeof(IndexValue));
            var first = (IndexValue)result.Values[0];
            Assert.AreEqual("name", first.Name);
            Assert.AreEqual(2, first.Values.Count);
            Assert.IsInstanceOfType(first.Values[0], typeof(StringValue));
            Assert.IsInstanceOfType(first.Values[1], typeof(StringValue));
        }
    }
}
