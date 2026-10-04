/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Search;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;
using Spark.Engine.Utility;
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
        TestContext context = new(clearIndexOnRebuild: false, migrationVersion: 0);

        DatabaseMigrationException exception =
            await Assert.ThrowsAsync<DatabaseMigrationException>(() => context.Service.RebuildIndexAsync());

        Assert.Contains(nameof(IndexSettings.ClearIndexOnRebuild), exception.Message, StringComparison.Ordinal);
        context.IndexStore.Verify(store => store.CleanAsync(), Times.Never);
        context.EntryReader.Verify(reader => reader.ReadAsync(It.IsAny<FhirStorePageReaderOptions>()), Times.Never);
    }

    [Fact]
    public async Task PendingArrayMigrationRequiresClearingBeforeRebuildStarts()
    {
        TestContext context = new(clearIndexOnRebuild: false, migrationVersion: 1);

        DatabaseMigrationException exception =
            await Assert.ThrowsAsync<DatabaseMigrationException>(() => context.Service.RebuildIndexAsync());

        Assert.Contains(DatabaseMigrations.TokenQuantityAndReferenceArrayIndex.Name, exception.Message, StringComparison.Ordinal);
        context.IndexStore.Verify(store => store.CleanAsync(), Times.Never);
        context.EntryReader.Verify(reader => reader.ReadAsync(It.IsAny<FhirStorePageReaderOptions>()), Times.Never);
    }

    [Fact]
    public async Task SuccessfulRebuildRecordsPendingMigration()
    {
        TestContext context = new(clearIndexOnRebuild: true, migrationVersion: 0);

        await context.Service.RebuildIndexAsync();

        context.IndexStore.Verify(store => store.CleanAsync(), Times.Once);
        context.MigrationService.Verify(
            service => service.RecordCompletedAsync(
                DatabaseMigrations.StructuredStringTokenIndex,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
        context.MigrationService.Verify(
            service => service.RecordCompletedAsync(
                DatabaseMigrations.TokenQuantityAndReferenceArrayIndex,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SuccessfulRebuildRecordsPendingMigrationsInVersionOrder()
    {
        TestContext context = new(clearIndexOnRebuild: true, migrationVersion: 0);
        List<int> recordedVersions = [];
        context.MigrationService
            .Setup(service => service.RecordCompletedAsync(
                It.IsAny<DatabaseMigration>(),
                It.IsAny<CancellationToken>()
            ))
            .Callback<DatabaseMigration, CancellationToken>((migration, _) => recordedVersions.Add(migration.Version))
            .Returns(Task.CompletedTask);

        await context.Service.RebuildIndexAsync();

        Assert.Equal([1, 2], recordedVersions);
    }

    [Fact]
    public async Task IndexingFailureLeavesPendingMigrationUnrecorded()
    {
        Entry entry = Entry.Create(
            new Key("http://localhost/", "Patient", "patient-1", "1"),
            new Patient { Id = "patient-1" }
        );
        TestContext context = new(clearIndexOnRebuild: true, migrationVersion: 0, entries: [entry]);
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
    public async Task BatchIndexingFailureLeavesPendingMigrationUnrecorded()
    {
        Entry entry = Entry.Create(
            new Key("http://localhost/", "Patient", "patient-1", "1"),
            new Patient { Id = "patient-1" }
        );
        Mock<IIndexService2> batchIndexService = new();
        batchIndexService
            .Setup(service => service.ProcessBatchAsync(It.IsAny<IReadOnlyList<Entry>>()))
            .ReturnsAsync([new IndexBatchFailure
            {
                Entry = entry,
                Exception = new InvalidOperationException("Bulk write failed.")
            }]);
        TestContext context = new(
            clearIndexOnRebuild: true,
            migrationVersion: 0,
            entries: [entry],
            indexService: batchIndexService.Object
        );

        await context.Service.RebuildIndexAsync();

        batchIndexService.Verify(
            service => service.ProcessBatchAsync(It.Is<IReadOnlyList<Entry>>(entries => entries.Count == 1)),
            Times.Once
        );
        context.MigrationService.Verify(
            service => service.RecordCompletedAsync(It.IsAny<DatabaseMigration>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task InvalidResourceIsSkippedAndPendingMigrationsAreRecorded()
    {
        Entry invalidEntry = Entry.Create(
            new Key("http://localhost/", "Observation", "decimal", "1"),
            CreateObservationWithOutOfRangeDecimal()
        );
        Entry validEntry = Entry.Create(
            new Key("http://localhost/", "Patient", "patient-1", "1"),
            new Patient { Id = "patient-1" }
        );
        TestContext context = new(clearIndexOnRebuild: true, migrationVersion: 0, entries: [invalidEntry, validEntry]);
        context.IndexService
            .Setup(service => service.ProcessAsync(invalidEntry))
            .Returns(() =>
            {
                // Accessing the value throws like it does when the resource is indexed.
                _ = ((Quantity)((Observation)invalidEntry.Resource).Component[0].Value).Value;
                return Task.CompletedTask;
            });

        await context.Service.RebuildIndexAsync();

        context.IndexService.Verify(service => service.ProcessAsync(validEntry), Times.Once);
        context.MigrationService.Verify(
            service => service.RecordCompletedAsync(
                DatabaseMigrations.StructuredStringTokenIndex,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
        context.MigrationService.Verify(
            service => service.RecordCompletedAsync(
                DatabaseMigrations.TokenQuantityAndReferenceArrayIndex,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task MigrationPersistenceFailureFailsRebuild()
    {
        TestContext context = new(clearIndexOnRebuild: true, migrationVersion: 0);
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
        TestContext context = new(clearIndexOnRebuild: false, migrationVersion: 2);

        await context.Service.RebuildIndexAsync();

        context.IndexStore.Verify(store => store.CleanAsync(), Times.Never);
        context.EntryReader.Verify(reader => reader.ReadAsync(It.IsAny<FhirStorePageReaderOptions>()), Times.Once);
        context.MigrationService.Verify(
            service => service.RecordCompletedAsync(It.IsAny<DatabaseMigration>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    private static Observation CreateObservationWithOutOfRangeDecimal()
    {
        const string json = """
            {
              "resourceType": "Observation",
              "id": "decimal",
              "status": "final",
              "code": { "text": "Decimal Testing Observation" },
              "component": [
                {
                  "code": { "text": "Component" },
                  "valueQuantity": { "value": -1.000000000000000000e245, "unit": "g" }
                }
              ]
            }
            """;

        // Resources are read back from the store in Ostrich mode, which keeps the out of range value as a string.
        return new FhirJsonDeserializer(DeserializerSettingsFactory.GetOstrichDeserializerSettings())
            .Deserialize<Observation>(json);
    }

    private sealed class TestContext
    {
        public TestContext(
            bool clearIndexOnRebuild,
            int migrationVersion,
            IElementIndexer2 elementIndexer = null,
            IReadOnlyList<Entry> entries = null,
            IIndexService indexService = null)
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
                .Setup(service => service.IsApplied(It.IsAny<int>()))
                .Returns((int version) => migrationVersion >= version);

            Service = new IndexRebuildService(
                IndexStore.Object,
                indexService ?? IndexService.Object,
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
