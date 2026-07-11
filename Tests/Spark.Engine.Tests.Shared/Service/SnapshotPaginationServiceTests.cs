/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Tests.Service;

public class SnapshotPaginationServiceTests
{
    private const string BaseUrl = "http://localhost/fhir";

    private static ISnapshotPagination CreateService(Snapshot snapshot, Entry[] entries = null)
    {
        var mockFhirIndex = new Mock<IFhirIndex>();
        var mockFhirStore = new Mock<IFhirStore>();
        var mockTransfer = new Mock<ITransfer>();
        var mockLocalhost = new Mock<ILocalhost>();
        var calculator = new SnapshotPaginationCalculator();

        mockLocalhost
            .Setup(l => l.Absolute(It.IsAny<Uri>()))
            .Returns<Uri>(u => u.IsAbsoluteUri ? u : new Uri(new Uri(BaseUrl), u));

        mockFhirStore
            .Setup(store => store.GetAsync(It.IsAny<IEnumerable<IKey>>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(entries ?? []);

        return new SnapshotPaginationService(
            mockFhirIndex.Object,
            mockFhirStore.Object,
            mockTransfer.Object,
            mockLocalhost.Object,
            calculator,
            snapshot,
            new FhirModel());
    }

    private static ISnapshotPagination CreateService(Snapshot snapshot, IFhirStore fhirStore)
    {
        var mockFhirIndex = new Mock<IFhirIndex>();
        var mockTransfer = new Mock<ITransfer>();
        var mockLocalhost = new Mock<ILocalhost>();
        var calculator = new SnapshotPaginationCalculator();

        mockLocalhost
            .Setup(l => l.Absolute(It.IsAny<Uri>()))
            .Returns<Uri>(u => u.IsAbsoluteUri ? u : new Uri(new Uri(BaseUrl), u));

        return new SnapshotPaginationService(
            mockFhirIndex.Object,
            fhirStore,
            mockTransfer.Object,
            mockLocalhost.Object,
            calculator,
            snapshot,
            new FhirModel());
    }

    [Fact]
    public async Task GetPageAsync_WhenIsCountOnly_ReturnsBundleWithTotalSet()
    {
        const int expectedCount = 42;
        var snapshot = Snapshot.CreateCountOnly(Bundle.BundleType.Searchset, new Uri($"{BaseUrl}/Patient"), count: 42);
        var service = CreateService(snapshot);

        var bundle = await service.GetPageAsync();

        Assert.Equal(expectedCount, bundle.Total);
    }

    [Fact]
    public async Task GetPageAsync_WhenIsCountOnly_ReturnsEmptyEntries()
    {
        var snapshot = Snapshot.CreateCountOnly(Bundle.BundleType.Searchset, new Uri($"{BaseUrl}/Patient"), count: 42);
        var service = CreateService(snapshot);

        var bundle = await service.GetPageAsync();

        Assert.Empty(bundle.Entry);
    }

    [Fact]
    public async Task GetPageAsync_WhenIsCountOnly_HasNoNavigationLinks()
    {
        var snapshot = Snapshot.CreateCountOnly(Bundle.BundleType.Searchset, new Uri($"{BaseUrl}/Patient"), count: 42);
        var service = CreateService(snapshot);

        var bundle = await service.GetPageAsync();

        Assert.Null(bundle.FirstLink);
        Assert.Null(bundle.LastLink);
        Assert.Null(bundle.NextLink);
        Assert.Null(bundle.PreviousLink);
    }

    [Fact]
    public async Task GetPageAsync_WhenIsCountOnly_HasSelfLink()
    {
        var snapshot = Snapshot.CreateCountOnly(Bundle.BundleType.Searchset, new Uri($"{BaseUrl}/Patient"), count: 42);
        var service = CreateService(snapshot);

        var bundle = await service.GetPageAsync();

        Assert.NotNull(bundle.SelfLink);
    }

    [Fact]
    public async Task GetPageAsync_WhenIsCountOnly_BundleTypeIsSearchset()
    {
        var snapshot = Snapshot.CreateCountOnly(Bundle.BundleType.Searchset, new Uri($"{BaseUrl}/Patient"), count: 42);
        var service = CreateService(snapshot);

        var bundle = await service.GetPageAsync();

        Assert.Equal(Bundle.BundleType.Searchset, bundle.Type);
    }

    [Fact]
    public async Task GetPageAsync_EntriesComesBackInTheSameOrderKeysWereStoredInTheSnapshot()
    {
        Snapshot snapshot = Snapshot.Create(
            Bundle.BundleType.Searchset,
            selfLink: new Uri($"{BaseUrl}/Patient"),
            keys:
            [
                "Patient/1/_history/1",
                "Patient/2/_history/1",
                "Patient/3/_history/1",
            ],
            sortBy: null,
            count: 10,
            includes: [],
            reverseIncludes: [],
            elements: []
        );
        Entry[] entries =
        [
            Entry.Create(
                Bundle.HTTPVerb.GET,
                Key.ParseOperationPath("Patient/3/_history/1"),
                new Patient { Id = "3" }
            ),
            Entry.Create(
                Bundle.HTTPVerb.GET,
                Key.ParseOperationPath("Patient/2/_history/1"),
                new Patient { Id = "2" }
            ),
            Entry.Create(
                Bundle.HTTPVerb.GET,
                Key.ParseOperationPath("Patient/1/_history/1"),
                new Patient { Id = "1" }
            ),
        ];
        ISnapshotPagination snapshotPagination = CreateService(snapshot, entries);

        Bundle bundle = await snapshotPagination.GetPageAsync();

        Assert.Equal(["1", "2", "3"], bundle.Entry.Select(entry => entry.Resource?.Id));
    }

    [Fact]
    public async Task GetPageAsync_WithSnapshotElements_ShouldPassElementsToFhirStore()
    {
        string[] expectedElements = ["id", "name"];
        Snapshot snapshot = Snapshot.Create(
            Bundle.BundleType.Searchset,
            selfLink: new Uri($"{BaseUrl}/Patient"),
            keys: ["Patient/1/_history/1"],
            sortBy: null,
            count: 10,
            includes: [],
            reverseIncludes: [],
            elements: expectedElements
        );
        var mockFhirStore = new Mock<IFhirStore>();
        mockFhirStore
            .Setup(store => store.GetAsync(It.IsAny<IEnumerable<IKey>>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(
            [
                Entry.Create(
                    Bundle.HTTPVerb.GET,
                    Key.ParseOperationPath("Patient/1/_history/1"),
                    new Patient { Id = "1" }
                )
            ]);
        var service = CreateService(snapshot, mockFhirStore.Object);

        await service.GetPageAsync();

        mockFhirStore.Verify(
            store => store.GetAsync(
                It.IsAny<IEnumerable<IKey>>(),
                It.Is<IEnumerable<string>>(elements => elements.SequenceEqual(expectedElements))),
            Times.Once);
    }
}
