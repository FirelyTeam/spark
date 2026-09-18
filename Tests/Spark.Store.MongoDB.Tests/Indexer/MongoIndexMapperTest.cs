/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Microsoft.Extensions.Logging.Abstractions;
using Spark.Engine.Model;
using Spark.Engine.Search.Types;
using Spark.Store.MongoDB.Search.Indexer;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Search;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Spark.Store.MongoDB.Tests.Indexer;

public class MongoIndexMapperTest
{
    private readonly MongoIndexMapper _indexMapper;
    private readonly ITestOutputHelper _output;

    public MongoIndexMapperTest(ITestOutputHelper output)
    {
        _indexMapper = new MongoIndexMapper();
        _output = output;
    }

    [Fact]
    public void RootIndexValueWillBeSkipped()
    {
        IndexValue rootIndexValue = new("root");
        List<BsonDocument> indexedEntries = _indexMapper.MapEntry(rootIndexValue);
        Assert.Empty(indexedEntries);
    }

    [Fact]
    public void MissingRootIndexValueWillThrowArgumentException()
    {
        IndexValue indexValue = new("not-root");
        Assert.Throws<ArgumentException>(() => _indexMapper.MapEntry(indexValue));
    }

    [Fact]
    public void MapEntryAddsIndexValueInternalLevelEqualToZeroForNonNestedValues()
    {
        IndexValue indexValue = new("root");
        indexValue.Values.Add(new IndexValue("internal_resource", new StringValue("Patient")));

        List<BsonDocument> indexedEntries = _indexMapper.MapEntry(indexValue);

        Assert.Single(indexedEntries);
        BsonDocument indexedEntry = indexedEntries[0];
        Assert.True(indexedEntry.IsBsonDocument);
        Assert.Equal(2, indexedEntry.ElementCount);
        BsonElement internalLevelElement = indexedEntry.GetElement(0);
        Assert.Equal("internal_level", internalLevelElement.Name);
        Assert.Equal(0, internalLevelElement.Value);
    }

    [Fact]
    public void MapEntryCanMapIndexValueWithStringValue()
    {
        IndexValue indexValue = new("root");
        indexValue.Values.Add(new IndexValue("internal_resource", new StringValue("Patient")));

        List<BsonDocument> indexedEntries = _indexMapper.MapEntry(indexValue);

        Assert.Single(indexedEntries);
        BsonDocument indexedEntry = indexedEntries[0];
        Assert.True(indexedEntry.IsBsonDocument);
        Assert.Equal(2, indexedEntry.ElementCount);
        BsonElement indexedInternalResource = indexedEntry.GetElement(1);
        Assert.Equal("internal_resource", indexedInternalResource.Name);
        Assert.True(indexedInternalResource.Value.IsString);
        Assert.Equal("Patient", indexedInternalResource.Value.AsString);
    }

    [Fact]
    public async Task MapEntryUsesAnArrayForSearchParamTypeTokenWithOneValue()
    {
        BsonDocument document = await MapExamplePatientAsync("patient-map-entry.json");

        _output.WriteLine(document.ToJson(new JsonWriterSettings { Indent = true }));
        Assert.True(document.Contains("identifier"));
        Assert.True(document["identifier"].IsBsonArray);
        Assert.Single(document["identifier"].AsBsonArray);
    }

    [Fact]
    public async Task MapEntryUsesAnArrayForSearchParamTypeTokenWithTwoValues()
    {
        BsonDocument document = await MapExamplePatientAsync("patient-map-entry-two-identifiers.json");

        _output.WriteLine(document.ToJson(new JsonWriterSettings { Indent = true }));
        Assert.True(document["identifier"].IsBsonArray);
        Assert.Equal(2, document["identifier"].AsBsonArray.Count);
    }

    [Fact]
    public void MapEntryUsesAnArrayForSearchParamTypeQuantityWithOneValue()
    {
        IndexValue root = new("root");
        IndexValue quantity = new("value-quantity")
        {
            SearchParamType = SearchParamType.Quantity
        };

        quantity.Values.Add(new CompositeValue(
        [
            new IndexValue("system", new StringValue("http://unitsofmeasure.org")),
            new IndexValue("value", new NumberValue(2.0m)),
            new IndexValue("decimals", new StringValue("2")),
            new IndexValue("unit", new StringValue("mmol"))
        ]));

        root.Values.Add(quantity);

        BsonDocument document = Assert.Single(_indexMapper.MapEntry(root));
        BsonArray quantityValues = Assert.IsType<BsonArray>(
            document["value-quantity"]);

        BsonDocument quantityValue = Assert.IsType<BsonDocument>(
            Assert.Single(quantityValues));

        Assert.Equal("http://unitsofmeasure.org", quantityValue["system"].AsString);
        Assert.Equal(2.0, quantityValue["value"].AsDouble);
        Assert.Equal("2", quantityValue["decimals"].AsString);
        Assert.Equal("mmol", quantityValue["unit"].AsString);
    }

    [Fact]
    public void MapEntryUsesSeparateArrayElementsForSearchParamTypeQuantityWithTwoValues()
    {
        IndexValue root = new("root");
        IndexValue quantity = new("value-quantity")
        {
            SearchParamType = SearchParamType.Quantity
        };

        quantity.Values.AddRange(
        [
            new CompositeValue(
            [
                new IndexValue("system", new StringValue("http://unitsofmeasure.org")),
                new IndexValue("value", new NumberValue(2.0m)),
                new IndexValue("decimals", new StringValue("2")),
                new IndexValue("unit", new StringValue("mmol"))
            ]),
            new CompositeValue(
            [
                new IndexValue("system", new StringValue("http://unitsofmeasure.org")),
                new IndexValue("value", new NumberValue(3.0m)),
                new IndexValue("decimals", new StringValue("3")),
                new IndexValue("unit", new StringValue("mmol"))
            ])
        ]);

        root.Values.Add(quantity);

        BsonDocument document = Assert.Single(_indexMapper.MapEntry(root));
        BsonArray quantityValues = Assert.IsType<BsonArray>(
            document["value-quantity"]);

        Assert.Equal(2, quantityValues.Count);

        BsonDocument firstQuantity = Assert.IsType<BsonDocument>(quantityValues[0]);
        Assert.Equal(2.0, firstQuantity["value"].AsDouble);
        Assert.Equal("2", firstQuantity["decimals"].AsString);
        Assert.Equal("mmol", firstQuantity["unit"].AsString);

        BsonDocument secondQuantity = Assert.IsType<BsonDocument>(quantityValues[1]);
        Assert.Equal(3.0, secondQuantity["value"].AsDouble);
        Assert.Equal("3", secondQuantity["decimals"].AsString);
        Assert.Equal("mmol", secondQuantity["unit"].AsString);
    }

    [Fact]
    public void MapEntryUsesAnArrayForSearchParamTypeReferenceWithOneValue()
    {
        IndexValue root = new("root");
        IndexValue reference = new("subject", new StringValue("Patient/p1"))
        {
            SearchParamType = SearchParamType.Reference
        };

        root.Values.Add(reference);

        BsonDocument document = Assert.Single(_indexMapper.MapEntry(root));
        BsonArray referenceValues = Assert.IsType<BsonArray>(document["subject"]);

        Assert.Equal("Patient/p1", Assert.Single(referenceValues).AsString);
    }

    [Fact]
    public void MapEntryUsesAnArrayForSearchParamTypeReferenceWithOneIdentifierValue()
    {
        IndexValue root = new("root");
        IndexValue reference = new("subject")
        {
            SearchParamType = SearchParamType.Reference
        };

        reference.Values.Add(new CompositeValue(
        [
            new IndexValue("code", new StringValue("p1")),
            new IndexValue("system", new StringValue("http://example.org/patients"))
        ]));

        root.Values.Add(reference);

        BsonDocument document = Assert.Single(_indexMapper.MapEntry(root));
        BsonArray referenceValues = Assert.IsType<BsonArray>(document["subject"]);

        BsonDocument identifier = Assert.IsType<BsonDocument>(Assert.Single(referenceValues));
        Assert.Equal("p1", identifier["code"].AsString);
        Assert.Equal("http://example.org/patients", identifier["system"].AsString);
    }

    private static async Task<BsonDocument> MapExamplePatientAsync(string fileName)
    {
        string json = await File.ReadAllTextAsync(Path.Combine("Examples", fileName), TestContext.Current.CancellationToken);
        Patient patient = new FhirJsonDeserializer().Deserialize<Patient>(json);
        FhirModel fhirModel = new();
        Mock<IIndexStore> indexStore = new();
        IndexService indexService = new(
            fhirModel,
            indexStore.Object,
            new ElementIndexer(fhirModel),
            new ResourceResolver(fhirModel.SupportedResources, new PocoStructureDefinitionSummaryProvider()),
            new NullLogger<IndexService>()
        );

        IndexValue indexValue = await indexService.IndexResourceAsync(
            patient,
            new Key("http://localhost/", "Patient", patient.Id, "3")
        );

        return Assert.Single(new MongoIndexMapper().MapEntry(indexValue));
    }
}
