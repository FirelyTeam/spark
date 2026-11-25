/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Spark.Engine.Store.Interfaces;
using Spark.Web.Tests.Stores;
using Task = System.Threading.Tasks.Task;

namespace Spark.Web.Tests;

// Every host's AddFhir adds to the static ModelInfo.SearchParameters, so this host must not start alongside another one.
[CollectionDefinition(nameof(ReadElementsTests), DisableParallelization = true)]
public class ReadElementsTestsCollection;

/// <summary>
/// The general parameter _elements applies to read and vread as well as search (https://github.com/FirelyTeam/spark/issues/1336).
/// </summary>
[Collection(nameof(ReadElementsTests))]
public class ReadElementsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SubsettedSystem = "http://terminology.hl7.org/CodeSystem/v3-ObservationValue";
    private const string SubsettedCode = "SUBSETTED";

    private readonly WebApplicationFactory<Program> _factory;

    public ReadElementsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Nothing here may reach MongoDB, so point the store at a port where nothing listens.
            builder.UseSetting("StoreSettings:ConnectionString", "mongodb://localhost:1/spark?serverSelectionTimeoutMS=2000");
            builder.ConfigureTestServices(services =>
            {
                // Keep the test away from MongoDB: no background services, and a store that holds one patient.
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IFhirStore>();
                services.AddSingleton<IFhirStore>(new SinglePatientStore(CreatePatient()));
            });
        });
    }

    [Theory]
    [InlineData("json")]
    [InlineData("xml")]
    public async Task Read_WithoutElements_ReturnsTheWholeResource(string format)
    {
        Patient patient = await ReadPatientAsync($"fhir/Patient/example?_format={format}", format);

        Assert.NotEmpty(patient.Name);
        Assert.Equal("1970-01-01", patient.BirthDate);
        Assert.Equal(AdministrativeGender.Female, patient.Gender);
        Assert.NotEmpty(patient.Address);
        Assert.DoesNotContain(patient.Meta?.Tag ?? [], tag => tag is { System: SubsettedSystem, Code: SubsettedCode });
    }

    [Theory]
    [InlineData("json")]
    [InlineData("xml")]
    public async Task Read_WithElements_ReturnsOnlyTheRequestedElements(string format)
    {
        Patient patient = await ReadPatientAsync($"fhir/Patient/example?_format={format}&_elements=name,birthDate", format);

        AssertSubsetted(patient);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("xml")]
    public async Task VRead_WithElements_ReturnsOnlyTheRequestedElements(string format)
    {
        Patient patient = await ReadPatientAsync($"fhir/Patient/example/_history/1?_format={format}&_elements=name,birthDate", format);

        AssertSubsetted(patient);
    }

    [Fact]
    public async Task Read_WithElementsAndSummary_LetsSummaryWin()
    {
        // With _summary=true, _elements is ignored. Patient.gender is in the summary, so it stays.
        Patient patient = await ReadPatientAsync("fhir/Patient/example?_format=json&_summary=true&_elements=name", "json");

        Assert.Equal(AdministrativeGender.Female, patient.Gender);
    }

    /// <summary>
    /// Only name and birthDate were asked for. The id stays, Patient.active stays because it is a modifier element,
    /// and the resource is tagged as SUBSETTED once, with the same coding search uses.
    /// </summary>
    private static void AssertSubsetted(Patient patient)
    {
        Assert.Equal("example", patient.Id);
        Assert.NotEmpty(patient.Name);
        Assert.Equal("1970-01-01", patient.BirthDate);
        Assert.True(patient.Active);
        Assert.Null(patient.Gender);
        Assert.Empty(patient.Address);
        Assert.NotNull(patient.Meta);
        Coding tag = Assert.Single(patient.Meta.Tag, tag => tag is { System: SubsettedSystem, Code: SubsettedCode });
        Assert.Equal("subsetted", tag.Display);
    }

    private async Task<Patient> ReadPatientAsync(string requestUri, string format)
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync();
        return format == "xml"
            ? new FhirXmlParser().Parse<Patient>(body)
            : new FhirJsonParser().Parse<Patient>(body);
    }

    private static Patient CreatePatient() => new()
    {
        Id = "example",
        Meta = new Meta { VersionId = "1", LastUpdated = DateTimeOffset.UtcNow },
        Active = true,
        Name = [new HumanName { Family = "Example", Given = ["Eve"] }],
        BirthDate = "1970-01-01",
        Gender = AdministrativeGender.Female,
        Address = [new Address { City = "Oslo" }]
    };
}
