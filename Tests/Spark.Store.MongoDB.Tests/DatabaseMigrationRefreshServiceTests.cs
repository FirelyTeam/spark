/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.Extensions.Logging;
using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Spark.Store.MongoDB.Tests;

public partial class DatabaseMigrationRefreshServiceTests
{
    [Fact]
    public async Task StartAsync_RefreshesImmediately()
    {
        TaskCompletionSource refreshed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubMigrationService migrationService = new((_, _) =>
        {
            refreshed.TrySetResult();
            return Task.CompletedTask;
        });
        DatabaseMigrationRefreshService worker = CreateWorker(migrationService, TimeSpan.FromHours(1));

        await worker.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await refreshed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(1, migrationService.RefreshCount);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ExecuteAsync_RefreshesPeriodically()
    {
        TaskCompletionSource refreshedTwice = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubMigrationService migrationService = new((count, _) =>
        {
            if (count >= 2)
                refreshedTwice.TrySetResult();

            return Task.CompletedTask;
        });
        DatabaseMigrationRefreshService worker = CreateWorker(migrationService, TimeSpan.FromMilliseconds(10));

        await worker.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await refreshedTwice.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(migrationService.RefreshCount >= 2);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StopAsync_CancelsActiveRefresh()
    {
        TaskCompletionSource refreshStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource refreshCancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubMigrationService migrationService = new(async (_, cancellationToken) =>
        {
            refreshStarted.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                refreshCancelled.TrySetResult();
                throw;
            }
        });
        DatabaseMigrationRefreshService worker = CreateWorker(migrationService, TimeSpan.FromHours(1));

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await refreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);

        await refreshCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshFails_LogsAndContinuesRefreshing()
    {
        TaskCompletionSource secondRefresh = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubMigrationService migrationService = new((count, _) =>
            {
                if (count == 1)
                    throw new InvalidOperationException("Refresh failed.");

                secondRefresh.TrySetResult();
                return Task.CompletedTask;
            }
        );
        migrationService.CurrentVersion = 2;
        TestLogger<DatabaseMigrationRefreshService> logger = new();
        DatabaseMigrationRefreshService worker = new(
            migrationService,
            logger,
            TimeSpan.FromMilliseconds(10));

        await worker.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await secondRefresh.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            Assert.Equal(2, migrationService.CurrentVersion);
            Assert.Contains(
                logger.Entries,
                entry =>
                    entry.Level == LogLevel.Error &&
                    entry.Exception is InvalidOperationException &&
                    entry.Message.Contains("Retaining version 2", StringComparison.Ordinal)
            );
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private static DatabaseMigrationRefreshService CreateWorker(
        IDatabaseMigrationService migrationService,
        TimeSpan refreshInterval)
    {
        return new DatabaseMigrationRefreshService(
            migrationService,
            new TestLogger<DatabaseMigrationRefreshService>(),
            refreshInterval
        );
    }

    private sealed class StubMigrationService : IDatabaseMigrationService
    {
        private readonly Func<int, CancellationToken, Task> _refresh;
        private int _refreshCount;

        public StubMigrationService(Func<int, CancellationToken, Task> refresh)
        {
            _refresh = refresh;
        }

        public int CurrentVersion { get; set; }

        public int RefreshCount => Volatile.Read(ref _refreshCount);

        public bool IsApplied(int version) => version > 0 && version <= CurrentVersion;

        public Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            int count = Interlocked.Increment(ref _refreshCount);
            return _refresh(count, cancellationToken);
        }

        public Task RecordCompletedAsync(
            DatabaseMigration migration,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
