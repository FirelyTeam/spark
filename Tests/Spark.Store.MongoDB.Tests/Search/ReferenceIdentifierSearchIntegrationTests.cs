/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Search;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using Spark.Store.MongoDB.Search;
using Spark.Store.MongoDB.Search.Common;
using Spark.Store.MongoDB.Search.Indexer;
using System;
using System.Threading.Tasks;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Store.MongoDB.Tests.Search;

[Trait("Category", "Integration")]
[Collection("MongoDB integration")]
public class ReferenceIdentifierSearchIntegrationTests
{
    private const string BaseUri = "http://localhost/";
    private readonly MongoDbFixture _mongo;

    public ReferenceIdentifierSearchIntegrationTests(MongoDbFixture mongo) => _mongo = mongo;

    [Theory]
    [InlineData(0)] // No migrations applied
    [InlineData(1)] // StructuredStringTokenIndex
    [InlineData(2)] // StructuredStringTokenIndex | TokenQuantityAndReferenceArrayIndex
    public async Task Reference_Identifier_Search_Returns_Matching_Observation(int appliedMigrationVersion)
    {
        Resource[] resources =
        [
            CreateObservation("o1", "http://example.org/patients", "p1"),
            CreateObservation("o2", "http://example.org/patients", "p2")
        ];
        MongoSearcher searcher = await SeedStoreAndReturnSearcherAsync(
            _mongo.CreateConnectionString("reference-identifier"),
            appliedMigrationVersion,
            resources);

        SearchResults results = await searcher.SearchAsync("Observation",
            new SearchParams().Add("subject:identifier", "http://example.org/patients|p1"));

        Assert.False(results.HasErrors);
        Assert.Equal(1, results.MatchCount);
        string result = Assert.Single(results);
        Assert.Equal("http://localhost/Observation/o1/_history/1", result);
    }

    private static async Task<MongoSearcher> SeedStoreAndReturnSearcherAsync(
        string connectionString,
        int appliedMigrationVersion,
        Resource[] resources)
    {
        IFhirModel fhirModel = new FhirModel();
        ILocalhost localhost = new Localhost(new Uri(BaseUri));
        MongoIndexStore indexStore = new(connectionString, new MongoIndexMapper(), new NullLogger<MongoIndexStore>());
        IndexService indexService = new(
            fhirModel,
            indexStore,
            new ElementIndexer(fhirModel),
            new ResourceResolver(fhirModel.SupportedResources, new PocoStructureDefinitionSummaryProvider()),
            new NullLogger<IndexService>()
        );
        Mock<IDatabaseMigrationService> migrationService = new();
        migrationService
            .Setup(service => service.IsApplied(It.IsAny<int>()))
            .Returns((int version) => appliedMigrationVersion >= version);
        MongoSearcher searcher = new(
            indexStore,
            localhost,
            fhirModel,
            new ReferenceNormalizationService(localhost),
            migrationService.Object);

        foreach (Resource resource in resources)
        {
            await indexService.IndexResourceAsync(
                resource,
                new Key(BaseUri, resource.TypeName, resource.Id, "1"));
        }

        return searcher;
    }

    private static Observation CreateObservation(string id, string system, string value) =>
        new()
        {
            Id = id,
            Status = ObservationStatus.Final,
            Code = new CodeableConcept("http://loinc.org", "1234-5"),
            Subject = new ResourceReference { Identifier = new Identifier(system, value) }
        };
}
