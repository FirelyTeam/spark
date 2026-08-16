/* 
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.Extensions.Logging;
using Spark.Engine.Core;
using Spark.Engine.Maintenance;
using Spark.Engine.Search;
using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;
using System;
using System.Threading.Tasks;

namespace Spark.Engine.Service.FhirServiceExtensions;

public class IndexRebuildService : IIndexRebuildService
{
    private readonly IIndexStore _indexStore;
    private readonly IIndexService _indexService;
    private readonly IFhirStorePagedReader _entryReader;
    private readonly SparkSettings _sparkSettings;
    private readonly IDatabaseMigrationService _databaseMigrationService;
    private readonly IElementIndexer2 _elementIndexer;
    private readonly ILogger<IndexRebuildService> _logger;

    public IndexRebuildService(
        IIndexStore indexStore,
        IIndexService indexService,
        IFhirStorePagedReader entryReader,
        SparkSettings sparkSettings,
        IDatabaseMigrationService databaseMigrationService,
        IElementIndexer2 elementIndexer,
        ILogger<IndexRebuildService> logger)
    {
        _indexStore = indexStore ?? throw new ArgumentNullException(nameof(indexStore));
        _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
        _entryReader = entryReader ?? throw new ArgumentNullException(nameof(entryReader));
        _sparkSettings = sparkSettings ?? throw new ArgumentNullException(nameof(sparkSettings));
        _databaseMigrationService = databaseMigrationService ?? throw new ArgumentNullException(nameof(databaseMigrationService));
        _elementIndexer = elementIndexer ?? throw new ArgumentNullException(nameof(elementIndexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Obsolete("Use IndexRebuildService(IIndexStore, IIndexService, IFhirStorePagedReader, SparkSettings, IDatabaseMigrationService, IElementIndexer, ILogger<IndexRebuildService> instead.)")]
    public IndexRebuildService(
        IIndexStore indexStore,
        IIndexService indexService,
        IFhirStorePagedReader entryReader,
        SparkSettings sparkSettings)
    {
        _indexStore = indexStore ?? throw new ArgumentNullException(nameof(indexStore));
        _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
        _entryReader = entryReader ?? throw new ArgumentNullException(nameof(entryReader));
        _sparkSettings = sparkSettings ?? throw new ArgumentNullException(nameof(sparkSettings));
    }

    public async Task RebuildIndexAsync(IIndexBuildProgressReporter reporter = null)
    {
        using (MaintenanceMode.Enable(MaintenanceLockMode.Write)) // allow to read data while reindexing
        {
            var indexSettings = _sparkSettings.IndexSettings ?? new IndexSettings();
            bool structuredStringTokenIndexPending = _databaseMigrationService != null &&
                !_databaseMigrationService.IsApplied(DatabaseMigrations.StructuredStringTokenIndex.Version);

            if (structuredStringTokenIndexPending)
            {
                ValidateStructuredStringTokenIndexMigration(indexSettings);
            }

            var progress = new IndexRebuildProgress(reporter);
            await progress.StartedAsync().ConfigureAwait(false);

            // TODO: lock collections for writing somehow?

            if (indexSettings.ClearIndexOnRebuild)
            {
                await progress.CleanStartedAsync().ConfigureAwait(false);
                await _indexStore.CleanAsync().ConfigureAwait(false);
                await progress.CleanCompletedAsync().ConfigureAwait(false);
            }

            var paging = await _entryReader.ReadAsync(new FhirStorePageReaderOptions
            {
                PageSize = indexSettings.ReindexBatchSize
            }).ConfigureAwait(false);

            bool hasIndexingFailures = false;
            await paging.IterateAllPagesAsync(async entries =>
            {
                // Selecting records page-by-page (page size is defined in app config, default is 100).
                // This will help to keep memory usage under control.
                foreach (var entry in entries)
                {
                    // TODO: use BulkWrite operation for this
                    try
                    {
                        await _indexService.ProcessAsync(entry).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        hasIndexingFailures = true;
                        _logger.LogError(exception, "Failed to reindex entry {EntryKey}", entry.Key);
                        await progress.ErrorAsync($"Error: Failed to reindex entry {entry.Key}");
                    }
                }

                await progress.RecordsProcessedAsync(entries.Count, paging.TotalRecords)
                    .ConfigureAwait(false);

            }).ConfigureAwait(false);

            if (structuredStringTokenIndexPending && !hasIndexingFailures)
            {
                await _databaseMigrationService
                    .RecordCompletedAsync(DatabaseMigrations.StructuredStringTokenIndex)
                    .ConfigureAwait(false);
            }

            // TODO: - unlock collections for writing

            await progress.DoneAsync()
                .ConfigureAwait(false);
        }
    }

    private void ValidateStructuredStringTokenIndexMigration(IndexSettings indexSettings)
    {
        if (_elementIndexer is null)
        {
            throw new DatabaseMigrationException(
                $"Database migration '{DatabaseMigrations.StructuredStringTokenIndex.Name}' requires an " +
                $"{nameof(IElementIndexer2)} implementation."
            );
        }

        if (!indexSettings.ClearIndexOnRebuild)
        {
            throw new DatabaseMigrationException(
                $"Database migration '{DatabaseMigrations.StructuredStringTokenIndex.Name}' requires " +
                $"{nameof(IndexSettings.ClearIndexOnRebuild)}=true."
            );
        }
    }
}

internal class IndexRebuildProgress
{
    private const int INDEX_CLEAR_PROGRESS_PERCENTAGE = 10;

    private readonly IIndexBuildProgressReporter _reporter;
    private int _overallProgress;
    private int _remainingProgress = 100;
    private int _recordsProcessed;

    public IndexRebuildProgress(IIndexBuildProgressReporter reporter)
    {
        _reporter = reporter;
    }

    public async Task StartedAsync()
    {
        await ReportProgressAsync("Index rebuild started")
            .ConfigureAwait(false);
    }

    public async Task CleanStartedAsync()
    {
        await ReportProgressAsync("Clearing index")
            .ConfigureAwait(false);
    }

    public async Task CleanCompletedAsync()
    {
        _overallProgress += INDEX_CLEAR_PROGRESS_PERCENTAGE;
        await ReportProgressAsync("Index cleared")
            .ConfigureAwait(false);
        _remainingProgress -= _overallProgress;
    }

    public async Task RecordsProcessedAsync(int records, long total)
    {
        _recordsProcessed += records;
        _overallProgress += (int)(_remainingProgress / (double)total * records);
        await ReportProgressAsync($"{_recordsProcessed} records processed")
            .ConfigureAwait(false);
    }

    public async Task DoneAsync()
    {
        _overallProgress = 100;
        await ReportProgressAsync("Index rebuild done")
            .ConfigureAwait(false);
    }

    public async Task ErrorAsync(string error)
    {
        if (_reporter == null)
        {
            return;
        }
        await _reporter.ReportErrorAsync(error)
            .ConfigureAwait(false);
    }

    public async Task ErrorAsync(Exception exception)
    {
        await ErrorAsync(exception.Message)
            .ConfigureAwait(false);
    }

    private async Task ReportProgressAsync(string message)
    {
        if (_reporter == null)
        {
            return;
        }
        await _reporter.ReportProgressAsync(_overallProgress, message)
            .ConfigureAwait(false);
    }
}
