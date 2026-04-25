/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spark.Engine.Core;
using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Service;

public class IndexWorker : BackgroundService
{
    private readonly IIndexQueue _indexQueue;
    private readonly IIndexService _indexService;
    private readonly ILogger<IndexWorker> _logger;
    private readonly IndexQueueSettings _settings;
    private readonly string _workerId;

    public IndexWorker(
        IIndexQueue indexQueue,
        IIndexService indexService,
        ILogger<IndexWorker> logger,
        IndexQueueSettings settings)
    {
        _indexQueue = indexQueue;
        _indexService = indexService;
        _logger = logger;
        _settings = settings;
        _workerId = $"{Environment.MachineName}:{Environment.ProcessId}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IndexWorker {WorkerId} starting.", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            IndexQueueEntry entry = null;
            try
            {
                entry = await _indexQueue.ClaimNextAsync(stoppingToken).ConfigureAwait(false);

                if (entry is null)
                {
                    await Task.Delay(_settings.PollInterval, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                await _indexService.ProcessAsync(entry.Entry).ConfigureAwait(false);
                await _indexQueue.AcknowledgeAsync(entry.Id, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (entry is null)
                    continue;

                _logger.LogWarning(ex, "IndexWorker {WorkerId} failed to process entry {EntryId} (attempt {Attempts}).",
                    _workerId, entry.Id, entry.Attempts);
                try
                {
                    await _indexQueue.NackAsync(entry.Id, ex.Message, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception nackEx)
                {
                    _logger.LogError(nackEx, "IndexWorker {WorkerId} failed to nack entry {EntryId}.",
                        _workerId, entry.Id);
                }
            }
        }

        _logger.LogInformation("IndexWorker {WorkerId} stopping.", _workerId);
    }
}
