/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Search;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Mongo.Search.Common;
using Spark.Mongo.Search.Indexer;
using Spark.Search.Mongo;
using System;
using System.Threading;
using Testcontainers.MongoDb;
using Xunit;
using Assert = Xunit.Assert;
using Task = System.Threading.Tasks.Task;

namespace Spark.Mongo.Tests.Search;

[Trait("Category", "Integration")]
public class QuantitySearchIntegrationTests
{
    private const string BaseUri = "http://localhost/";

    [SkippableFact]
    public async Task Quantity_search_with_unit_only_returns_matching_observation()
    {
        Skip.IfNot(OperatingSystem.IsLinux());

        var container = await StartMongoOrSkipAsync();
        try
        {
            Resource[] resources =
            [
                CreateObservation("o2mmol", 2.0m, "mmol", string.Empty),
                CreateObservation("o3mmol", 3.0m, "mmol", string.Empty),
                CreateObservation("o2g", 2.0m, "g", string.Empty)
            ];
            var searcher = await SeedStoreAndReturnSearcherAsync(container, resources);

            var results = await searcher.SearchAsync("Observation",
                new SearchParams().Add("value-quantity", "2.0||mmol"));

            Assert.False(results.HasErrors);
            Assert.Equal(1, results.MatchCount);
            Assert.Single(results);
            Assert.Equal("http://localhost/Observation/o2mmol/_history/1", results[0]);
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Quantity_search_with_unit_only_and_gt_returns_greater_matching_observation()
    {
        Skip.IfNot(OperatingSystem.IsLinux());

        var container = await StartMongoOrSkipAsync();
        try
        {
            Resource[] resources =
            [
                CreateObservation("o2mmol", 2.0m, "mmol", null),
                CreateObservation("o3mmol", 3.0m, "mmol", null),
                CreateObservation("o2g", 2.0m, "g", null)
            ];
            var searcher = await SeedStoreAndReturnSearcherAsync(container, resources);

            var results = await searcher.SearchAsync("Observation",
                new SearchParams().Add("value-quantity", "gt2.0||mmol"));

            Assert.False(results.HasErrors);
            Assert.Equal(1, results.MatchCount);
            Assert.Single(results);
            Assert.Equal("http://localhost/Observation/o3mmol/_history/1", results[0]);
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Quantity_search_with_code_only_returns_matching_observation()
    {
        Skip.IfNot(OperatingSystem.IsLinux());

        var container = await StartMongoOrSkipAsync();
        try
        {
            Resource[] resources =
            [
                CreateObservation("o2mmol", 2.0m, string.Empty, "mmol"),
                CreateObservation("o3mmol", 3.0m, string.Empty, "mmol"),
                CreateObservation("o2g", 2.0m, string.Empty, "g")
            ];
            var searcher = await SeedStoreAndReturnSearcherAsync(container, resources);

            var results = await searcher.SearchAsync("Observation",
                new SearchParams().Add("value-quantity", "2.0||mmol"));

            Assert.False(results.HasErrors);
            Assert.Equal(1, results.MatchCount);
            Assert.Single(results);
            Assert.Equal("http://localhost/Observation/o2mmol/_history/1", results[0]);
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Quantity_search_with_code_only_and_gt_returns_greater_matching_observation()
    {
        Skip.IfNot(OperatingSystem.IsLinux());

        var container = await StartMongoOrSkipAsync();
        try
        {
            Resource[] resources =
            [
                CreateObservation("o2mmol", 2.0m, null, "mmol"),
                CreateObservation("o3mmol", 3.0m, null, "mmol"),
                CreateObservation("o2g", 2.0m, null, "g")
            ];
            var searcher = await SeedStoreAndReturnSearcherAsync(container, resources);

            var results = await searcher.SearchAsync("Observation",
                new SearchParams().Add("value-quantity", "gt2.0||mmol"));

            Assert.False(results.HasErrors);
            Assert.Equal(1, results.MatchCount);
            Assert.Single(results);
            Assert.Equal("http://localhost/Observation/o3mmol/_history/1", results[0]);
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    private static async System.Threading.Tasks.Task<MongoDbContainer> StartMongoOrSkipAsync()
    {
        MongoDbContainer container = null;
        try
        {
            container = new MongoDbBuilder("mongo:8.2.7").Build();
            await container.StartAsync(CancellationToken.None);
            return container;
        }
        catch (Exception ex)
        {
            if (container != null)
            {
                await container.DisposeAsync();
            }

            Skip.If(true, $"Docker/Testcontainers not available: {ex.Message}");
            return null;
        }
    }

    private static async System.Threading.Tasks.Task<MongoSearcher> SeedStoreAndReturnSearcherAsync(MongoDbContainer container,
        Resource[] resources)
    {
        var connectionString = BuildConnectionString(container.GetConnectionString(), "sparktest");

        IFhirModel fhirModel = new FhirModel();
        ILocalhost localhost = new Localhost(new Uri(BaseUri));
        var indexStore = new MongoIndexStore(connectionString, new MongoIndexMapper());
        var indexService = new IndexService(fhirModel, indexStore, new ElementIndexer(fhirModel), new ResourceResolver());
        var searcher = new MongoSearcher(indexStore, localhost, fhirModel, new ReferenceNormalizationService(localhost));

        foreach (Resource resource in resources)
        {
            await indexService.IndexResourceAsync(resource, new Key(BaseUri, resource.TypeName, resource.Id, "1"));
        }

        return searcher;
    }

    private static Observation CreateObservation(string id, decimal value, string unit, string code) =>
        new Observation
        {
            Id = id,
            Value = new Quantity
            {
                Value = value,
                System = "http://unitsofmeasure.org",
                Code = code,
                Unit = unit
            }
        };

    private static string BuildConnectionString(string raw, string databaseName)
    {
        var builder = new MongoUrlBuilder(raw) { DatabaseName = databaseName };
        if (!string.IsNullOrEmpty(builder.Username) && string.IsNullOrEmpty(builder.AuthenticationSource))
        {
            builder.AuthenticationSource = "admin";
        }

        return builder.ToMongoUrl().ToString();
    }
}
