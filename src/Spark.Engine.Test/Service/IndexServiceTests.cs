/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Model;
using Spark.Engine.Search;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using Spark.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Hl7.Fhir.Model.ModelInfo;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Test.Service;

[TestClass]
public class IndexServiceTests
{
    private IndexService _limitedIndexService;
    private IndexService _fullIndexService;
    private string _examplePatientJson;
    private string _exampleAppointmentJson;
    private string _carePlanWithContainedGoal;
    private string _exampleObservationJson;

    [TestInitialize]
    public void TestInitialize()
    {
        Mock<IIndexStore> indexStoreMock = new Mock<IIndexStore>();
        _examplePatientJson = TextFileHelper.ReadTextFileFromDisk($".{Path.DirectorySeparatorChar}Examples{Path.DirectorySeparatorChar}patient-example.json");
        _exampleAppointmentJson = TextFileHelper.ReadTextFileFromDisk($".{Path.DirectorySeparatorChar}Examples{Path.DirectorySeparatorChar}appointment-example2doctors.json");
        _carePlanWithContainedGoal = TextFileHelper.ReadTextFileFromDisk($".{Path.DirectorySeparatorChar}Examples{Path.DirectorySeparatorChar}careplan-example-f201-renal.json");
        _exampleObservationJson = TextFileHelper.ReadTextFileFromDisk($".{Path.DirectorySeparatorChar}Examples{Path.DirectorySeparatorChar}observation-example-bloodpressure.json");
        var spPatientName = new SearchParamDefinition
        {
            Resource = "Patient",
            Name = "name",
            Description = new Markdown(@"A portion of either family or given name of the patient"),
            Type = SearchParamType.String,
            Path = new string[] { "Patient.name" },
            Expression = "Patient.name"
        };
        var spMiddleName = new SearchParamDefinition
        {
            Resource = "Patient",
            Name = "middlename",
            Type = SearchParamType.String,
            Path = new string[] { "Patient.name.extension.where(url='http://hl7.no/fhir/StructureDefinition/no-basis-middlename')" },
            Expression = "Patient.name.extension.where(url='http://hl7.no/fhir/StructureDefinition/no-basis-middlename')"
        };
        var searchParameters = new List<SearchParamDefinition> { spPatientName, spMiddleName };
        var resources = new Dictionary<Type, string> { { typeof(Patient), "Patient" }, { typeof(HumanName), "HumanName" } };

        var resourceResolver = new ResourceResolver();
            
        // For this test setup we want a limited available types and search parameters.
        IFhirModel limitedFhirModel = new FhirModel(resources, searchParameters);
        ElementIndexer limitedElementIndexer = new ElementIndexer(limitedFhirModel);
        _limitedIndexService = new IndexService(limitedFhirModel, indexStoreMock.Object, limitedElementIndexer, resourceResolver);

        // For this test setup we want all available types and search parameters.
        IFhirModel fullFhirModel = new FhirModel();
        ElementIndexer fullElementIndexer = new ElementIndexer(fullFhirModel);
        _fullIndexService = new IndexService(fullFhirModel, indexStoreMock.Object, fullElementIndexer, resourceResolver);
    }
        
    [TestMethod]
    public async Task TestIndexCustomSearchParameter()
    {
        var patient = new Patient();
        HumanName name = new HumanName().WithGiven("Adriaan").AndFamily("Bestevaer");
        name.AddExtension("http://hl7.no/fhir/StructureDefinition/no-basis-middlename", new FhirString("Michel"));
        patient.Name.Add(name);

        IKey patientKey = new Key("http://localhost/", "Patient", "002", "1");
        IndexValue result = await _limitedIndexService.IndexResourceAsync(patient, patientKey);

        var middleName = result.NonInternalValues().Skip(1).First();
        Assert.AreEqual("middlename", middleName.Name);
        Assert.AreEqual(1, middleName.Values.Count());
        Assert.IsInstanceOfType(middleName.Values[0], typeof(StringValue));
        Assert.AreEqual("Michel", middleName.Values[0].ToString());
    }

    [TestMethod]
    public async Task TestIndexResourceSimple()
    {
        var patient = new Patient();
        patient.Name.Add(new HumanName().WithGiven("Adriaan").AndFamily("Bestevaer"));

        IKey patientKey = new Key("http://localhost/", "Patient", "001", "v02");

        IndexValue result = await _limitedIndexService.IndexResourceAsync(patient, patientKey);

        Assert.AreEqual("root", result.Name);
        Assert.AreEqual(1, result.NonInternalValues().Count(), "Expected 1 non-internal result for searchparameter 'name'");
        var first = result.NonInternalValues().First();
        Assert.AreEqual("name", first.Name);
        Assert.AreEqual(2, first.Values.Count);
        Assert.IsInstanceOfType(first.Values[0], typeof(StringValue));
        Assert.IsInstanceOfType(first.Values[1], typeof(StringValue));
    }

    [TestMethod]
    public async Task TestIndexResourcePatientComplete()
    {
        FhirJsonParser parser = new FhirJsonParser();
        var patientResource = parser.Parse<Resource>(_examplePatientJson);

        IKey patientKey = new Key("http://localhost/", "Patient", "001", null);

        IndexValue result = await _fullIndexService.IndexResourceAsync(patientResource, patientKey);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task TestIndexResourceAppointmentComplete()
    {
        FhirJsonParser parser = new FhirJsonParser();
        var appResource = parser.Parse<Resource>(_exampleAppointmentJson);

        IKey appKey = new Key("http://localhost/", "Appointment", "2docs", null);

        IndexValue result = await _fullIndexService.IndexResourceAsync(appResource, appKey);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task TestIndexResourceCareplanWithContainedGoal()
    {
        FhirJsonParser parser = new FhirJsonParser();
        var cpResource = parser.Parse<Resource>(_carePlanWithContainedGoal);

        IKey cpKey = new Key("http://localhost/", "Careplan", "f002", null);

        IndexValue result = await _fullIndexService.IndexResourceAsync(cpResource, cpKey);

        Assert.IsNotNull(result);
    }


    [TestMethod]
    public async Task TestIndexResourceObservation()
    {
        FhirJsonParser parser = new FhirJsonParser();
        var obsResource = parser.Parse<Resource>(_exampleObservationJson);

        IKey cpKey = new Key("http://localhost/", "Observation", "blood-pressure", null);

        IndexValue result = await _fullIndexService.IndexResourceAsync(obsResource, cpKey);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task TestMultiValueIndexCanIndexFhirDateTime()
    {
        Condition cd = new Condition
        {
            Onset = new FhirDateTime(2015, 6, 15)
        };

        IKey cdKey = new Key("http://localhost/", "Condition", "test", null);

        IndexValue result = await _fullIndexService.IndexResourceAsync(cd, cdKey);

        Assert.IsNotNull(result);
        IndexValue onsetIndex = result.Values.Where(iv => (iv as IndexValue).Name == "onset-date").SingleOrDefault() as IndexValue;
        Assert.IsNotNull(onsetIndex);
    }

    [TestMethod]
    public async Task TestMultiValueIndexCanIndexFhirString()
    {
        string onsetInfo = "approximately November 2012";
        Condition cd = new Condition
        {
            Onset = new FhirString(onsetInfo)
        };

        IKey cdKey = new Key("http://localhost/", "Condition", "test", null);

        IndexValue result = await _fullIndexService.IndexResourceAsync(cd, cdKey);

        Assert.IsNotNull(result);
        IndexValue onsetIndex = result.Values.Where(iv => (iv as IndexValue).Name == "onset-info").SingleOrDefault() as IndexValue;
        Assert.IsNotNull(onsetIndex);
        Assert.IsTrue(onsetIndex.Values.Count == 1);
        Assert.IsTrue(onsetIndex.Values.First() is StringValue);
        Assert.AreEqual(onsetInfo, ((StringValue)onsetIndex.Values.First()).Value);
    }

    [TestMethod]
    public async Task TestMultiValueIndexCanIndexAge()
    {
        decimal onsetAge = 73;
        Condition cd = new Condition
        {
            Onset = new Age
            {
                System = "http://unitsofmeasure.org/",
                Code = "a",
                Value = onsetAge
            }
        };

        IKey cdKey = new Key("http://localhost/", "Condition", "test", null);

        IndexValue result = await _fullIndexService.IndexResourceAsync(cd, cdKey);

        Assert.IsNotNull(result);
        IndexValue onsetIndex = result.Values.Where(iv => (iv as IndexValue).Name == "onset-age").Single() as IndexValue;
        Assert.IsNotNull(onsetIndex);
        Assert.IsTrue(onsetIndex.Values.First() is CompositeValue);
        CompositeValue composite = onsetIndex.Values.First() as CompositeValue;
        Assert.IsTrue(composite.Components.Cast<IndexValue>().Where(c => c.Name == "value").First().Values.First() is NumberValue);
        NumberValue value = composite.Components.Cast<IndexValue>().Where(c => c.Name == "value").First().Values.First() as NumberValue;

        // TODO: Need to convert back to years for this to work, base unit of time is seconds if I am not mistaken.
        //Assert.AreEqual(onsetAge, value.Value);
    }
}