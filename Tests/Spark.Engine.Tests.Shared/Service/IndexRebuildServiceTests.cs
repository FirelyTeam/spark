/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Search;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Tests.Service;

public class IndexRebuildServiceTests
{
    [Fact]
    public async Task PendingMigrationRequiresClearingBeforeRebuildStarts()
    {
        TestContext context = new(clearIndexOnRebuild: false, migrationApplied: false);

        DatabaseMigrationException exception =
            await Assert.ThrowsAsync<DatabaseMigrationException>(() => context.Service.RebuildIndexAsync());

        Assert.Contains(nameof(IndexSettings.ClearIndexOnRebuild), exception.Message, StringComparison.Ordinal);
        context.IndexStore.Verify(store => store.CleanAsync(), Times.Never);
        context.EntryReader.Verify(reader => reader.ReadAsync(It.IsAny<FhirStorePageReaderOptions>()), Times.Never);
    }

    [Fact]
    public async Task SuccessfulRebuildRecordsPendingMigration()
    {
        TestContext context = new(clearIndexOnRebuild: true, migrationApplied: false);

        await context.Service.RebuildIndexAsync();

        context.IndexStore.Verify(store => store.CleanAsync(), Times.Once);
        context.MigrationService.Verify(
            service => service.RecordCompletedAsync(
                DatabaseMigrations.StructuredStringTokenIndex,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task IndexingFailureLeavesPendingMigrationUnrecorded()
    {
        Entry entry = Entry.Create(
            new Key("http://localhost/", "Patient", "patient-1", "1"),
            new Patient { Id = "patient-1" }
        );
        TestContext context = new(clearIndexOnRebuild: true, migrationApplied: false, entries: [entry]);
        context.IndexService
            .Setup(service => service.ProcessAsync(entry))
            .ThrowsAsync(new InvalidOperationException("Indexing failed."));

        await context.Service.RebuildIndexAsync();

        context.MigrationService.Verify(
            service => service.RecordCompletedAsync(It.IsAny<DatabaseMigration>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task MigrationPersistenceFailureFailsRebuild()
    {
        TestContext context = new(clearIndexOnRebuild: true, migrationApplied: false);
        context.MigrationService
            .Setup(service => service.RecordCompletedAsync(
                    DatabaseMigrations.StructuredStringTokenIndex,
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new DatabaseMigrationException("Persistence failed."));

        DatabaseMigrationException exception =
            await Assert.ThrowsAsync<DatabaseMigrationException>(() => context.Service.RebuildIndexAsync());

        Assert.Equal("Persistence failed.", exception.Message);
    }

    [Fact]
    public async Task AppliedMigrationPermitsNonClearingRebuild()
    {
        TestContext context = new(clearIndexOnRebuild: false, migrationApplied: true);

        await context.Service.RebuildIndexAsync();

        context.IndexStore.Verify(store => store.CleanAsync(), Times.Never);
        context.EntryReader.Verify(reader => reader.ReadAsync(It.IsAny<FhirStorePageReaderOptions>()), Times.Once);
        context.MigrationService.Verify(
            service => service.RecordCompletedAsync(It.IsAny<DatabaseMigration>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    private sealed class TestContext
    {
        public TestContext(
            bool clearIndexOnRebuild,
            bool migrationApplied,
            IElementIndexer2 elementIndexer = null,
            IReadOnlyList<Entry> entries = null)
        {
            entries ??= [];
            elementIndexer ??= new Mock<IElementIndexer2>().Object;

            PageResult.SetupGet(result => result.TotalRecords).Returns(entries.Count);
            PageResult
                .Setup(result => result.IterateAllPagesAsync(It.IsAny<Func<IReadOnlyList<Entry>, Task>>()))
                .Returns((Func<IReadOnlyList<Entry>, Task> callback) =>
                    entries.Count == 0 ? Task.CompletedTask : callback(entries)
                );
            EntryReader
                .Setup(reader => reader.ReadAsync(It.IsAny<FhirStorePageReaderOptions>()))
                .ReturnsAsync(PageResult.Object);
            MigrationService
                .Setup(service => service.IsApplied(DatabaseMigrations.StructuredStringTokenIndex.Version))
                .Returns(migrationApplied);

            Service = new IndexRebuildService(
                IndexStore.Object,
                IndexService.Object,
                EntryReader.Object,
                new SparkSettings
                {
                    IndexSettings = new IndexSettings { ClearIndexOnRebuild = clearIndexOnRebuild }
                },
                MigrationService.Object,
                elementIndexer,
                new Mock<ILogger<IndexRebuildService>>().Object
            );
        }

        public Mock<IIndexStore> IndexStore { get; } = new();

        public Mock<IIndexService> IndexService { get; } = new();

        public Mock<IFhirStorePagedReader> EntryReader { get; } = new();

        public Mock<IPageResult<Entry>> PageResult { get; } = new();

        public Mock<IDatabaseMigrationService> MigrationService { get; } = new();

        public IndexRebuildService Service { get; }
    }
}
