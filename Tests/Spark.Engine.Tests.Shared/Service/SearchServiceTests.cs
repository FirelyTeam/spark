/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Rest;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using System;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Tests.Service;

public class SearchServiceTests
{
    /// <summary>Patient.active changes what the resource means, so a subsetted Patient has to carry it.</summary>
    private const string ModifierElement = "active";

    [Fact]
    public async Task GetSnapshotAsync_WithElements_KeepsTheElementsThatAlwaysComeAlong()
    {
        SearchParams searchCommand = new();
        searchCommand.Elements.Add("birthDate");

        Snapshot snapshot = await CreateService().GetSnapshotAsync("Patient", searchCommand);

        Assert.Contains("birthDate", snapshot.Elements);
        Assert.Contains(ModifierElement, snapshot.Elements);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithElements_LeavesTheSearchParamsAlone()
    {
        // The caller may search again with the same parameters, and then has to get the same subset back.
        SearchParams searchCommand = new();
        searchCommand.Elements.Add("birthDate");
        SearchService service = CreateService();

        Snapshot first = await service.GetSnapshotAsync("Patient", searchCommand);
        Snapshot second = await service.GetSnapshotAsync("Patient", searchCommand);

        Assert.Equal(new[] { "birthDate" }, searchCommand.Elements);
        Assert.Equal(first.Elements, second.Elements);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithElements_DoesNotCarryOneTypesElementsToAnother()
    {
        // Patient.deceased has no business in an Observation response.
        SearchParams searchCommand = new();
        searchCommand.Elements.Add("birthDate");
        SearchService service = CreateService();

        await service.GetSnapshotAsync("Patient", searchCommand);
        Snapshot observations = await service.GetSnapshotAsync("Observation", searchCommand);

        Assert.DoesNotContain("deceased", observations.Elements);
        Assert.Contains("status", observations.Elements);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithoutElements_AsksForWholeResources()
    {
        Snapshot snapshot = await CreateService().GetSnapshotAsync("Patient", new SearchParams());

        Assert.True(snapshot.Elements == null || snapshot.Elements.Count == 0);
    }

    private static SearchService CreateService()
    {
        SearchResults results = new() { "Patient/1" };
        Mock<IFhirIndex> index = new();
        index.Setup(i => i.SearchAsync(It.IsAny<string>(), It.IsAny<SearchParams>())).ReturnsAsync(results);

        // Localhost.Uri is an extension method, which Moq cannot stand in for.
        return new SearchService(new Localhost(new Uri("http://localhost/fhir")), new FhirModel(), index.Object);
    }
}
