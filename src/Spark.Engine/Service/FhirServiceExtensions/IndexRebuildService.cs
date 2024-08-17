/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Spark.Engine.Core;
using Spark.Engine.Maintenance;
using Spark.Engine.Store.Interfaces;
using System;
using System.Threading.Tasks;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class IndexRebuildService : IIndexRebuildService
    {
        private readonly IIndexStore _indexStore;
        private readonly IIndexService _indexService;
        private readonly IFhirStorePagedReader _entryReader;
        private readonly SparkSettings _sparkSettings;

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
                var progress = new IndexRebuildProgress(reporter);
                await progress.StartedAsync().ConfigureAwait(false);

                // TODO: lock collections for writing somehow?

                var indexSettings = _sparkSettings.IndexSettings ?? new IndexSettings();
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
                        catch (Exception)
                        {
                            // TODO: log exception!
                            await progress.ErrorAsync($"Failed to reindex entry {entry.Key}");
                        }
                    }

                    await progress.RecordsProcessedAsync(entries.Count, paging.TotalRecords)
                        .ConfigureAwait(false);

                }).ConfigureAwait(false);

                // TODO: - unlock collections for writing

                await progress.DoneAsync()
                    .ConfigureAwait(false);
            }
        }
    }

    internal class IndexRebuildProgress
    {
        private const int INDEX_CLEAR_PROGRESS_PERCENTAGE = 10;

        private readonly IIndexBuildProgressReporter _reporter;
        private int _overallProgress;
        private int _remainingProgress = 100;
        private int _recordsProcessed = 0;

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
}
