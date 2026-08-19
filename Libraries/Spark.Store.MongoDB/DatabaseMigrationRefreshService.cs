/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spark.Engine.Store.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Spark.Store.MongoDB;

internal sealed class DatabaseMigrationRefreshService : BackgroundService
{
    private static readonly TimeSpan DefaultRefreshInterval = TimeSpan.FromSeconds(30);

    private readonly IDatabaseMigrationService _migrationService;
    private readonly ILogger<DatabaseMigrationRefreshService> _logger;
    private readonly TimeSpan _refreshInterval;

    public DatabaseMigrationRefreshService(
        IDatabaseMigrationService migrationService,
        ILogger<DatabaseMigrationRefreshService> logger)
        : this(migrationService, logger, DefaultRefreshInterval)
    {
    }

    internal DatabaseMigrationRefreshService(
        IDatabaseMigrationService migrationService,
        ILogger<DatabaseMigrationRefreshService> logger,
        TimeSpan refreshInterval)
    {
        ArgumentNullException.ThrowIfNull(migrationService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(refreshInterval, TimeSpan.Zero);

        _migrationService = migrationService;
        _logger = logger;
        _refreshInterval = refreshInterval;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken).ConfigureAwait(false);
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(_refreshInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await RefreshAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RefreshAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _migrationService.RefreshAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to refresh database migration state. Retaining version {CurrentVersion}.",
                _migrationService.CurrentVersion
            );
        }
    }
}
