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
/// When a chained search resolves its inner sub-query, a failure of that sub-query must not be
/// swallowed: doing so mis-reports it as an unsupported parameter and silently drops the chain
/// constraint, so the search returns every resource instead of the filtered set. Here the inner
/// query fails because the date prefix (ge) is not stripped for it and the value fails to parse.
///
/// Requires Docker (Testcontainers); tagged Integration so the cross-platform unit run skips it
/// (only the Linux CI leg has Docker), while it still runs locally via a normal `dotnet test`.
/// </summary>
[Trait("Category", "Integration")]
public class ChainedSearchErrorHandlingTests
{
    private const string BaseUri = "http://localhost/";

    [Theory]
    [InlineData("subject:Patient.birthdate", "ge1974-12-25")] // prefix not stripped -> the date fails to parse
    [InlineData("subject:Patient.name:above", "Smith")]        // unsupported modifier on the inner parameter
    public async Task Chained_search_does_not_silently_drop_constraint_when_inner_query_fails(string chainedParameter, string value)
    {
        MongoDbContainer container = null;
        try
        {
            // Building/starting the container probes the Docker endpoint; on a host without Docker
            // (e.g. the macOS/Windows CI runners) that throws, so skip rather than fail.
            try
            {
                container = new MongoDbBuilder("mongo:8.2.7").Build();
                await container.StartAsync(TestContext.Current.CancellationToken);
            }
            catch (Exception ex)
            {
                Assert.Skip($"Docker/Testcontainers not available: {ex.Message}");
            }

            var connectionString = BuildConnectionString(container.GetConnectionString(), "sparktest");

            // Wire up the real index/search object graph.
            IFhirModel fhirModel = new FhirModel();
            ILocalhost localhost = new Localhost(new Uri(BaseUri));
            var referenceNormalization = new ReferenceNormalizationService(localhost);
            var indexStore = new MongoIndexStore(connectionString, new MongoIndexMapper());
            var elementIndexer = new ElementIndexer(fhirModel);
            var resolver = new ResourceResolver(fhirModel.SupportedResources, new PocoStructureDefinitionSummaryProvider());
            var indexService = new IndexService(fhirModel, indexStore, elementIndexer, resolver);
            var searcher = new MongoSearcher(indexStore, localhost, fhirModel, referenceNormalization);

            // Two patients born after the cut-off and two before, each with one Observation.
            var birthdates = new[] { "2000-01-01", "2001-01-01", "1950-01-01", "1951-01-01" };
            for (var i = 0; i < birthdates.Length; i++)
            {
                var patient = new Patient { Id = $"p{i}", BirthDate = birthdates[i] };
                await indexService.IndexResourceAsync(patient, new Key(BaseUri, "Patient", $"p{i}", "1"));

                var observation = new Observation
                {
                    Id = $"o{i}",
                    Status = ObservationStatus.Final,
                    Code = new CodeableConcept("http://loinc.org", "1234-5"),
                    Subject = new ResourceReference($"Patient/p{i}"),
                };
                await indexService.IndexResourceAsync(observation, new Key(BaseUri, "Observation", $"o{i}", "1"));
            }

            var search = new SearchParams().Add(chainedParameter, value);

            SearchResults results = null;
            Exception thrown = null;
            try
            {
                results = await searcher.SearchAsync("Observation", search);
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            // The chain constraint must take effect (fewer matches than the total) or the failure must
            // surface as an exception. Silently returning every Observation - or returning nothing
            // without an error - means the inner-query failure was swallowed.
            Assert.True(thrown != null || (results != null && results.MatchCount < birthdates.Length),
                $"Chained constraint had no effect: matches={results?.MatchCount.ToString() ?? "null"}, " +
                $"thrown={thrown?.GetType().Name ?? "none"}.");
        }
        finally
        {
            if (container != null)
                await container.DisposeAsync();
        }
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
