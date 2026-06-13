/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
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
using Testcontainers.MongoDb;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Store.MongoDB.Tests.Search;

/// <summary>
/// End-to-end behaviour of a chained search whose inner sub-query targets Patient. Both tests seed
/// four Observations - two whose Patient is born after the cut-off, two before. Needs Docker
/// (Testcontainers); tagged Integration so the cross-platform unit run skips it, while it still runs
/// locally via a normal `dotnet test`.
/// </summary>
[Trait("Category", "Integration")]
public class ChainedSearchErrorHandlingTests
{
    private const string BaseUri = "http://localhost/";

    /// <summary>
    /// A comparator prefix on the inner parameter (Patient.birthdate=ge...) is stripped and applied, so
    /// only the two Patients born after the cut-off match - leaving exactly two of the four Observations.
    /// </summary>
    [Fact]
    public async Task Chained_search_applies_comparator_prefix_on_inner_parameter()
    {
        var container = await StartMongoOrSkipAsync();
        try
        {
            var searcher = await SeedSearcherAsync(container);

            var results = await searcher.SearchAsync("Observation",
                new SearchParams().Add("subject:Patient.birthdate", "ge1974-12-25"));

            Assert.Equal(2, results.MatchCount);
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    /// <summary>
    /// The same comparator prefix on an untyped chain (subject.birthdate, no explicit :Patient type) is
    /// resolved against the reference's target and applied, again leaving exactly two of four Observations.
    /// </summary>
    [Fact]
    public async Task Chained_search_applies_comparator_prefix_on_untyped_inner_parameter()
    {
        var container = await StartMongoOrSkipAsync();
        try
        {
            var searcher = await SeedSearcherAsync(container);

            var results = await searcher.SearchAsync("Observation",
                new SearchParams().Add("subject.birthdate", "ge1974-12-25"));

            Assert.Equal(2, results.MatchCount);
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    /// <summary>
    /// An unsupported modifier on the inner parameter (Patient.name:above) makes the inner sub-query
    /// fail; that failure must surface as an exception, not be swallowed into an unfiltered result.
    /// </summary>
    [Fact]
    public async Task Chained_search_throws_when_inner_parameter_has_unsupported_modifier()
    {
        var container = await StartMongoOrSkipAsync();
        try
        {
            var searcher = await SeedSearcherAsync(container);

            await Assert.ThrowsAsync<ArgumentException>(() => searcher.SearchAsync("Observation",
                new SearchParams().Add("subject:Patient.name:above", "Smith")));
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    /// <summary>
    /// Reference checking rewrites a non-chained reference search into an internal chained lookup.
    /// The inner lookup uses internal_justid for bare ids and internal_id for typed/full references.
    /// </summary>
    [Theory]
    [InlineData("subject", "p1")]
    [InlineData("subject:Patient", "p1")]
    [InlineData("subject", "Patient/p1")]
    public async Task Reference_search_with_reference_check_uses_internal_reference_lookup(string parameterName, string parameterValue)
    {
        var container = await StartMongoOrSkipAsync();
        try
        {
            var searcher = await SeedSearcherAsync(container);
            var searchSettings = new SearchSettings
            {
                CheckReferences = true,
                CheckReferencesFor = ["Observation.subject"]
            };

            var results = await searcher.SearchAsync("Observation",
                new SearchParams().Add(parameterName, parameterValue),
                searchSettings);

            Assert.False(results.HasErrors);
            Assert.Equal(1, results.MatchCount);
            Assert.Single(results);
            Assert.Equal("http://localhost/Observation/o1/_history/1", results[0]);
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    private static async System.Threading.Tasks.Task<MongoDbContainer> StartMongoOrSkipAsync()
    {
        // Building/starting the container probes the Docker endpoint; on a host without Docker
        // (e.g. the macOS/Windows CI runners) that throws, so skip rather than fail.
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
            return null; // unreachable: Assert.Skip throws.
        }
    }

    private static async System.Threading.Tasks.Task<MongoSearcher> SeedSearcherAsync(MongoDbContainer container)
    {
        var connectionString = BuildConnectionString(container.GetConnectionString(), "sparktest");

        // Wire up the real index/search object graph.
        IFhirModel fhirModel = new FhirModel();
        ILocalhost localhost = new Localhost(new Uri(BaseUri));
        var indexStore = new MongoIndexStore(connectionString, new MongoIndexMapper());
        var indexService = new IndexService(fhirModel, indexStore, new ElementIndexer(fhirModel),
            new ResourceResolver(fhirModel.SupportedResources, new PocoStructureDefinitionSummaryProvider()));
        var searcher = new MongoSearcher(indexStore, localhost, fhirModel, new ReferenceNormalizationService(localhost));

        // Two patients born after the cut-off and two before, each with one Observation.
        var birthdates = new[] { "2000-01-01", "2001-01-01", "1950-01-01", "1951-01-01" };
        for (var i = 0; i < birthdates.Length; i++)
        {
            await indexService.IndexResourceAsync(
                new Patient { Id = $"p{i}", BirthDate = birthdates[i] },
                new Key(BaseUri, "Patient", $"p{i}", "1"));
            await indexService.IndexResourceAsync(
                new Observation { Id = $"o{i}", Subject = new ResourceReference($"Patient/p{i}") },
                new Key(BaseUri, "Observation", $"o{i}", "1"));
        }

        return searcher;
    }

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
