/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Model;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Test.Service;

// RebuildIndexAsync enables the global maintenance mode, so this must not run in parallel with MaintenanceModeTests.
[Collection("MaintenanceMode")]
public class IndexRebuildServiceTests
{
    [Fact]
    public async Task RebuildIndexAsync_WhenAnEntryFailsToIndex_LogsTheExceptionAndContinues()
    {
        Entry failing = Entry.POST(Key.Create("Patient", "1"), new Patient { Id = "1" });
        Entry succeeding = Entry.POST(Key.Create("Patient", "2"), new Patient { Id = "2" });
        InvalidOperationException exception = new("boom");

        Mock<IIndexService> indexService = new();
        indexService.Setup(s => s.ProcessAsync(failing)).ThrowsAsync(exception);
        indexService.Setup(s => s.ProcessAsync(succeeding)).Returns(Task.CompletedTask);

        Mock<IPageResult<Entry>> page = new();
        page.SetupGet(p => p.TotalRecords).Returns(2);
        page.Setup(p => p.IterateAllPagesAsync(It.IsAny<Func<IReadOnlyList<Entry>, Task>>()))
            .Returns<Func<IReadOnlyList<Entry>, Task>>(callback => callback(new List<Entry> { failing, succeeding }));

        Mock<IFhirStorePagedReader> reader = new();
        reader.Setup(r => r.ReadAsync(It.IsAny<FhirStorePageReaderOptions>())).ReturnsAsync(page.Object);

        Mock<IIndexBuildProgressReporter> reporter = new();
        reporter.Setup(r => r.ReportProgressAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        reporter.Setup(r => r.ReportErrorAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        Mock<ILogger<IndexRebuildService>> logger = new();

        IndexRebuildService service = new(
            new Mock<IIndexStore>().Object,
            indexService.Object,
            reader.Object,
            new SparkSettings(),
            logger.Object);

        await service.RebuildIndexAsync(reporter.Object);

        logger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString().Contains("Patient/1")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        indexService.Verify(s => s.ProcessAsync(succeeding), Times.Once);
        reporter.Verify(r => r.ReportErrorAsync(It.Is<string>(m => m.Contains("Patient/1"))), Times.Once);
    }
}
