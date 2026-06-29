/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Moq;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using Spark.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Test.Service;

public class SnapshotPaginationServiceTests
{
    private const string BaseUrl = "http://localhost/fhir";

    [Fact]
    public async Task GetPageAsync_EntriesComesBackInTheSameOrderKeysWereStoredInTheSnapshot()
    {
        Snapshot snapshot = Snapshot.Create(
            Bundle.BundleType.Searchset,
            selflink: new Uri($"{BaseUrl}/Patient"),
            keys:
            [
                "Patient/1/_history/1",
                "Patient/2/_history/1",
                "Patient/3/_history/1",
            ],
            sortby: null,
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

    private static ISnapshotPagination CreateService(Snapshot snapshot, Entry[] entries)
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
            .ReturnsAsync((IEnumerable<IKey> identifiers, IEnumerable<string> _) => identifiers.Any() ? entries : []);

        return new SnapshotPaginationService(
            mockFhirIndex.Object,
            mockFhirStore.Object,
            mockTransfer.Object,
            mockLocalhost.Object,
            calculator,
            snapshot);
    }
}
