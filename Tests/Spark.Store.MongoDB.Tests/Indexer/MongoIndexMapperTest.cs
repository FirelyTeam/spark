/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
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
    public async Task MapEntryUsesAnObjectForSearchParamTypeTokenWithOneValue()
    {
        BsonDocument document = await MapExamplePatientAsync("patient-map-entry.json");

        _output.WriteLine(document.ToJson(new JsonWriterSettings { Indent = true }));
        Assert.True(document.Contains("identifier"));
        Assert.True(document["identifier"].IsBsonDocument);
    }

    [Fact]
    public async Task MapEntryUsesAnArrayForSearchParamTypeTokenWithTwoValues()
    {
        BsonDocument document = await MapExamplePatientAsync("patient-map-entry-two-identifiers.json");

        _output.WriteLine(document.ToJson(new JsonWriterSettings { Indent = true }));
        Assert.True(document["identifier"].IsBsonArray);
        Assert.Equal(2, document["identifier"].AsBsonArray.Count);
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
            new ResourceResolver(fhirModel.SupportedResources, new PocoStructureDefinitionSummaryProvider())
        );

        IndexValue indexValue = await indexService.IndexResourceAsync(
            patient,
            new Key("http://localhost/", "Patient", patient.Id, "3")
        );

        return Assert.Single(new MongoIndexMapper().MapEntry(indexValue));
    }
}
