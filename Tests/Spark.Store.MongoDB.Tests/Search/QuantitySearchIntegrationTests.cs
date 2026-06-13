/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Search;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Store.MongoDB.Search;
using Spark.Store.MongoDB.Search.Common;
using Spark.Store.MongoDB.Search.Indexer;
using System;
using System.Threading.Tasks;
using Testcontainers.MongoDb;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Store.MongoDB.Tests.Search;

[Trait("Category", "Integration")]
public class QuantitySearchIntegrationTests : IAsyncLifetime
{
    private const string BaseUri = "http://localhost/";
    private MongoDbContainer _container;

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    public async ValueTask InitializeAsync() => _container = await StartMongoOrSkipAsync();

    [Fact]
    public async Task Quantity_Search_With_Unit_Only_Returns_Matching_Observation()
    {
        Resource[] resources =
        [
            CreateObservation("o2mmol", 2.0m, "mmol", string.Empty),
            CreateObservation("o3mmol", 3.0m, "mmol", string.Empty),
            CreateObservation("o2g", 2.0m, "g", string.Empty)
        ];

        MongoSearcher searcher = await SeedStoreAndReturnSearcherAsync(_container, resources);

        SearchResults results = await searcher.SearchAsync("Observation",
            new SearchParams().Add("value-quantity", "2.0||mmol"));

        Assert.False(results.HasErrors);
        Assert.Equal(1, results.MatchCount);
        Assert.Single(results);
        Assert.Equal("http://localhost/Observation/o2mmol/_history/1", results[0]);
    }

    [Fact]
    public async Task Quantity_Search_With_Unit_Only_And_Gt_Returns_Greater_Matching_Observation()
    {
        Resource[] resources =
        [
            CreateObservation("o2mmol", 2.0m, "mmol", null),
            CreateObservation("o3mmol", 3.0m, "mmol", null),
            CreateObservation("o2g", 2.0m, "g", null)
        ];
        MongoSearcher searcher = await SeedStoreAndReturnSearcherAsync(_container, resources);

        SearchResults results = await searcher.SearchAsync("Observation",
            new SearchParams().Add("value-quantity", "gt2.0||mmol"));

        Assert.False(results.HasErrors);
        Assert.Equal(1, results.MatchCount);
        Assert.Single(results);
        Assert.Equal("http://localhost/Observation/o3mmol/_history/1", results[0]);
    }

    [Fact]
    public async Task Quantity_Search_With_Code_Only_Returns_Matching_Observation()
    {
        Resource[] resources =
        [
            CreateObservation("o2mmol", 2.0m, string.Empty, "mmol"),
            CreateObservation("o3mmol", 3.0m, string.Empty, "mmol"),
            CreateObservation("o2g", 2.0m, string.Empty, "g")
        ];

        MongoSearcher searcher = await SeedStoreAndReturnSearcherAsync(_container, resources);

        SearchResults results = await searcher.SearchAsync("Observation",
            new SearchParams().Add("value-quantity", "2.0||mmol"));

        Assert.False(results.HasErrors);
        Assert.Equal(1, results.MatchCount);
        Assert.Single(results);
        Assert.Equal("http://localhost/Observation/o2mmol/_history/1", results[0]);
    }

    [Fact]
    public async Task Quantity_Search_With_Code_Only_And_Gt_Returns_Greater_Matching_Observation()
    {
        Resource[] resources =
        [
            CreateObservation("o2mmol", 2.0m, null, "mmol"),
            CreateObservation("o3mmol", 3.0m, null, "mmol"),
            CreateObservation("o2g", 2.0m, null, "g")
        ];
        MongoSearcher searcher = await SeedStoreAndReturnSearcherAsync(_container, resources);

        SearchResults results = await searcher.SearchAsync("Observation",
            new SearchParams().Add("value-quantity", "gt2.0||mmol"));

        Assert.False(results.HasErrors);
        Assert.Equal(1, results.MatchCount);
        Assert.Single(results);
        Assert.Equal("http://localhost/Observation/o3mmol/_history/1", results[0]);
    }

    private static async Task<MongoDbContainer> StartMongoOrSkipAsync()
    {
        MongoDbContainer container = null;
        try
        {
            container = new MongoDbBuilder("mongo:8.2.7").Build();
            await container.StartAsync(TestContext.Current.CancellationToken);
            return container;
        }
        catch (Exception ex)
        {
            if (container != null)
                await container.DisposeAsync();
            Assert.Skip($"Docker/Testcontainers not available: {ex.Message}");
            return null;
        }
    }

    private static async Task<MongoSearcher> SeedStoreAndReturnSearcherAsync(MongoDbContainer container,
        Resource[] resources)
    {
        string connectionString = BuildConnectionString(container.GetConnectionString(), "sparktest");

        IFhirModel fhirModel = new FhirModel();
        ILocalhost localhost = new Localhost(new Uri(BaseUri));
        MongoIndexStore indexStore = new(connectionString, new MongoIndexMapper());
        IndexService indexService = new(fhirModel, indexStore, new ElementIndexer(fhirModel),
            new ResourceResolver(fhirModel.SupportedResources, new PocoStructureDefinitionSummaryProvider()));
        MongoSearcher searcher = new(indexStore, localhost, fhirModel, new ReferenceNormalizationService(localhost));

        foreach (Resource resource in resources)
        {
            await IndexResourceAsync(indexService, resource);
        }

        return searcher;
    }

    private static Observation CreateObservation(string id, decimal value, string unit, string code) =>
        new()
        {
            Id = id,
            Value = new Quantity { Value = value, System = "http://unitsofmeasure.org", Code = code, Unit = unit }
        };

    private static async Task IndexResourceAsync(IndexService indexService, Resource resource) =>
        await indexService.IndexResourceAsync(
            resource,
            new Key(BaseUri, resource.TypeName, resource.Id, "1"));

    private static string BuildConnectionString(string raw, string databaseName)
    {
        MongoUrlBuilder builder = new(raw) { DatabaseName = databaseName };
        if (!string.IsNullOrEmpty(builder.Username) && string.IsNullOrEmpty(builder.AuthenticationSource))
        {
            builder.AuthenticationSource = "admin";
        }

        return builder.ToMongoUrl().ToString();
    }
}
