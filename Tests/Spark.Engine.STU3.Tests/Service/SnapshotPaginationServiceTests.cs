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
using System;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Test.Service;

public class SnapshotPaginationServiceTests
{
    private const string BaseUrl = "http://localhost/fhir";

    private static ISnapshotPagination CreateService(Snapshot snapshot)
    {
        var mockFhirIndex = new Mock<IFhirIndex>();
        var mockFhirStore = new Mock<Store.Interfaces.IFhirStore>();
        var mockTransfer = new Mock<ITransfer>();
        var mockLocalhost = new Mock<ILocalhost>();
        var calculator = new SnapshotPaginationCalculator();

        mockLocalhost
            .Setup(l => l.Absolute(It.IsAny<Uri>()))
            .Returns<Uri>(u => u.IsAbsoluteUri ? u : new Uri(new Uri(BaseUrl), u));

        return new SnapshotPaginationService(
            mockFhirIndex.Object,
            mockFhirStore.Object,
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
}
