/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Model;
using Spark.Engine.Search;
using Spark.Engine.Search.Types;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Specification;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Tests.Service;

public class IndexServiceTests
{
    private IndexService _limitedIndexService;
    private IndexService _fullIndexService;
    private string _examplePatientJson;
    private string _exampleAppointmentJson;
    private string _carePlanWithContainedGoal;
    private string _exampleObservationJson;

    public IndexServiceTests()
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
            Description = @"A portion of either family or given name of the patient",
            Type = SearchParamType.String,
            Path = ["Patient.name"],
            Expression = "Patient.name"
        };
        var spMiddleName = new SearchParamDefinition
        {
            Resource = "Patient",
            Name = "middlename",
            Type = SearchParamType.String,
            Path = ["Patient.name.extension.where(url='http://hl7.no/fhir/StructureDefinition/no-basis-middlename')"],
            Expression = "Patient.name.extension.where(url='http://hl7.no/fhir/StructureDefinition/no-basis-middlename')"
        };
        var searchParameters = new List<SearchParamDefinition> { spPatientName, spMiddleName };
        var resources = new Dictionary<Type, string> { { typeof(Patient), "Patient" }, { typeof(HumanName), "HumanName" } };

        var resourceResolver = new ResourceResolver(new FhirModel().SupportedResources, new PocoStructureDefinitionSummaryProvider());
            
        // For this test setup we want a limited available types and search parameters.
        IFhirModel limitedFhirModel = new FhirModel(resources, searchParameters);
        ElementIndexer limitedElementIndexer = new ElementIndexer(limitedFhirModel);
        _limitedIndexService = new IndexService(limitedFhirModel, indexStoreMock.Object, limitedElementIndexer, resourceResolver);

        // For this test setup we want all available types and search parameters.
        IFhirModel fullFhirModel = new FhirModel();
        ElementIndexer fullElementIndexer = new ElementIndexer(fullFhirModel);
        _fullIndexService = new IndexService(fullFhirModel, indexStoreMock.Object, fullElementIndexer, resourceResolver);
    }
        
    [Fact]
    public async Task TestIndexCustomSearchParameter()
    {
        var patient = new Patient();
        HumanName name = new HumanName().WithGiven("Adriaan").AndFamily("Bestevaer");
        name.AddExtension("http://hl7.no/fhir/StructureDefinition/no-basis-middlename", new FhirString("Michel"));
        patient.Name.Add(name);

        IKey patientKey = new Key("http://localhost/", "Patient", "002", "1");
        IndexValue result = await _limitedIndexService.IndexResourceAsync(patient, patientKey);

        var middleName = result.NonInternalValues().Skip(1).First();
        Assert.Equal("middlename", middleName.Name);
        Assert.Single(middleName.Values);
        Assert.IsType<StringValue>(middleName.Values[0]);
        Assert.Equal("Michel", middleName.Values[0].ToString());
    }

    [Fact]
    public async Task TestIndexResourceSimple()
    {
        var patient = new Patient();
        patient.Name.Add(new HumanName().WithGiven("Adriaan").AndFamily("Bestevaer"));

        IKey patientKey = new Key("http://localhost/", "Patient", "001", "v02");

        IndexValue result = await _limitedIndexService.IndexResourceAsync(patient, patientKey);

        Assert.Equal("root", result.Name);
        Assert.Single(result.NonInternalValues());
        var first = result.NonInternalValues().First();
        Assert.Equal("name", first.Name);
        Assert.Equal(2, first.Values.Count);
        Assert.IsType<StringValue>(first.Values[0]);
        Assert.IsType<StringValue>(first.Values[1]);
    }

    [Fact]
    public async Task TestIndexResourcePatientComplete()
    {
        FhirJsonDeserializer parser = new();
        var patientResource = parser.Deserialize<Resource>(_examplePatientJson);

        IKey patientKey = new Key("http://localhost/", "Patient", "001", null);

        IndexValue result = await _fullIndexService.IndexResourceAsync(patientResource, patientKey);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task TestIndexResourceAppointmentComplete()
    {
        FhirJsonDeserializer parser = new();
        var appResource = parser.Deserialize<Resource>(_exampleAppointmentJson);

        IKey appKey = new Key("http://localhost/", "Appointment", "2docs", null);

        IndexValue result = await _fullIndexService.IndexResourceAsync(appResource, appKey);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task TestIndexResourceCareplanWithContainedGoal()
    {
        FhirJsonDeserializer parser = new();
        var cpResource = parser.Deserialize<Resource>(_carePlanWithContainedGoal);

        IKey cpKey = new Key("http://localhost/", "Careplan", "f002", null);

        IndexValue result = await _fullIndexService.IndexResourceAsync(cpResource, cpKey);

        Assert.NotNull(result);
    }


    [Fact]
    public async Task TestIndexResourceObservation()
    {
        FhirJsonDeserializer parser = new();
        var obsResource = parser.Deserialize<Resource>(_exampleObservationJson);

        IKey cpKey = new Key("http://localhost/", "Observation", "blood-pressure", null);

        IndexValue result = await _fullIndexService.IndexResourceAsync(obsResource, cpKey);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task TestMultiValueIndexCanIndexFhirDateTime()
    {
        Condition cd = new Condition
        {
            Onset = new FhirDateTime(2015, 6, 15)
        };

        IKey cdKey = new Key("http://localhost/", "Condition", "test", null);

        IndexValue result = await _fullIndexService.IndexResourceAsync(cd, cdKey);

        Assert.NotNull(result);
        IndexValue onsetIndex = result.Values.SingleOrDefault(iv => (iv as IndexValue)?.Name == "onset-date") as IndexValue;
        Assert.NotNull(onsetIndex);
    }

    [Fact]
    public async Task TestMultiValueIndexCanIndexFhirString()
    {
        string onsetInfo = "approximately November 2012";
        Condition cd = new Condition
        {
            Onset = new FhirString(onsetInfo)
        };

        IKey cdKey = new Key("http://localhost/", "Condition", "test", null);

        IndexValue result = await _fullIndexService.IndexResourceAsync(cd, cdKey);

        Assert.NotNull(result);
        IndexValue onsetIndex = result.Values.SingleOrDefault(iv => (iv as IndexValue)?.Name == "onset-info") as IndexValue;
        Assert.NotNull(onsetIndex);
        Assert.Single(onsetIndex.Values);
        Assert.True(onsetIndex.Values.First() is StringValue);
        Assert.Equal(onsetInfo, ((StringValue)onsetIndex.Values.First()).Value);
    }

    [Fact]
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

        Assert.NotNull(result);
        IndexValue onsetIndex = result.Values.Single(iv => (iv as IndexValue)?.Name == "onset-age") as IndexValue;
        Assert.NotNull(onsetIndex);
        Assert.True(onsetIndex.Values.First() is CompositeValue);
        CompositeValue composite = onsetIndex.Values.FirstOrDefault() as CompositeValue;
        Assert.NotNull(composite);
        Assert.True(composite.Components.Cast<IndexValue>().First(c => c.Name == "value").Values.First() is NumberValue);
        NumberValue value = composite.Components.Cast<IndexValue>().First(c => c.Name == "value").Values.First() as NumberValue;

        Assert.NotNull(value);
        Assert.Equal(onsetAge, TimeSpan.FromSeconds((long)value.Value).Days / 365);
    }
}
