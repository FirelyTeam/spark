/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using Spark.Engine;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Store.MongoDB.Tests;

[Trait("Category", "Integration")]
[Collection("MongoDB integration")]
public class MongoFhirStoreIntegrationTests
{
    private const string SubsettedSystem = "http://terminology.hl7.org/CodeSystem/v3-ObservationValue";
    private const string SubsettedCode = "SUBSETTED";

    private readonly MongoDbFixture _mongo;

    public MongoFhirStoreIntegrationTests(MongoDbFixture mongo)
    {
        _mongo = mongo;

        ServiceCollection services = new();
        services.AddFhirWithMvc(new SparkSettings { Endpoint = new Uri("http://localhost/fhir") });
    }

    [Fact]
    public async Task GetAsync_WithElements_MarksResourceAsSubsetted()
    {
        MongoFhirStore store = CreateStore();
        await StorePatientAsync(store);

        var entries = await store.GetAsync([Key.Create("Patient", "patient-1")], ["birthDate"]);

        Patient patient = Assert.IsType<Patient>(Assert.Single(entries).Resource);
        Assert.Equal("1970-01-01", patient.BirthDate);
        Assert.Empty(patient.Name);
        Assert.NotNull(patient.Meta);
        Assert.Contains(patient.Meta.Tag, tag => tag is { System: SubsettedSystem, Code: SubsettedCode });
    }

    [Fact]
    public async Task GetAsync_WithoutElements_DoesNotMarkResourceAsSubsetted()
    {
        MongoFhirStore store = CreateStore();
        await StorePatientAsync(store);

        var entries = await store.GetAsync([Key.Create("Patient", "patient-1")], []);

        Patient patient = Assert.IsType<Patient>(Assert.Single(entries).Resource);
        Assert.Equal("1970-01-01", patient.BirthDate);
        Assert.Single(patient.Name);
        Assert.NotNull(patient.Meta);
        Assert.DoesNotContain(patient.Meta.Tag, tag => tag is { System: SubsettedSystem, Code: SubsettedCode });
    }

    private MongoFhirStore CreateStore() => new(_mongo.CreateConnectionString("fhirstore"));

    private static async Task StorePatientAsync(MongoFhirStore store)
    {
        Patient patient = new()
        {
            BirthDate = "1970-01-01",
            Name = [new HumanName { Family = "Example" }]
        };
        Entry entry = Entry.POST(Key.Create("Patient", "patient-1", "1"), patient);

        await store.AddAsync(entry);
    }
}
