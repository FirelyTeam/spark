/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Web.Tests;

// Every host started by a WebApplicationFactory calls AddFhir, which adds to the static ModelInfo.SearchParameters list.
// Two hosts starting at the same time can corrupt that list, so this host must not start alongside another one.
[CollectionDefinition(nameof(CapabilityStatementTests), DisableParallelization = true)]
public class CapabilityStatementTestsCollection;

[Collection(nameof(CapabilityStatementTests))]
public class CapabilityStatementTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CapabilityStatementTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // The CapabilityStatement needs no database. Point the store somewhere unreachable, so the test
            // behaves the same whether or not a MongoDB server happens to run locally, and don't create the
            // snapshot expiry index at startup, which is the only thing that connects to MongoDB on start.
            builder.UseSetting("StoreSettings:ConnectionString", "mongodb://localhost:1/spark?serverSelectionTimeoutMS=2000");
            builder.ConfigureTestServices(services =>
            {
                foreach (ServiceDescriptor descriptor in services
                    .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Name == "SnapshotExpiryIndexService")
                    .ToList())
                {
                    services.Remove(descriptor);
                }
            });
        });
    }

    [Fact]
    public async Task Metadata_ListsTheSearchParametersEveryResourceHas()
    {
        HttpClient client = _factory.CreateClient();
        HttpRequestMessage request = new(HttpMethod.Get, "/fhir/metadata");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));

        HttpResponseMessage response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        CapabilityStatement capabilityStatement = new FhirJsonParser().Parse<CapabilityStatement>(
            await response.Content.ReadAsStringAsync());
        CapabilityStatement.ResourceComponent patient = capabilityStatement.Rest.Single().Resource
            .Single(resource => resource.Type == "Patient");
        var searchParameters = patient.SearchParam.ToDictionary(parameter => parameter.Name, parameter => parameter.Type);

        Assert.Equal(SearchParamType.Uri, searchParameters["_source"]);
        Assert.Equal(SearchParamType.Token, searchParameters["_id"]);
        Assert.Equal(SearchParamType.String, searchParameters["family"]);
    }
}
