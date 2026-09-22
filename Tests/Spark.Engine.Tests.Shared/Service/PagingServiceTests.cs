/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using System;
using System.Net;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Tests.Service;

public class PagingServiceTests
{
    [Fact]
    public async Task StartPaginationAsync_WhenAChunkedSnapshotIsGone_TellsTheClientItIsGone()
    {
        Mock<ISnapshotStore2> store = new();
        store.Setup(s => s.GetSnapshotAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync((Snapshot)null);

        SparkException exception = await Assert.ThrowsAsync<SparkException>(
            () => new PagingService(store.Object, new Mock<ISnapshotPaginationProvider>().Object)
                .StartPaginationAsync("the-search-that-has-left-the-building", 100));

        Assert.Equal(HttpStatusCode.Gone, exception.StatusCode);
    }
}
