/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.Extensions.Logging;
using Moq;
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
        Mock<IDatabaseMigrationService> migrationService = new();
        migrationService
            .Setup(service => service.RefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                refreshed.TrySetResult();
                return Task.CompletedTask;
            });
        DatabaseMigrationRefreshService worker = CreateWorker(migrationService.Object, TimeSpan.FromHours(1));

        await worker.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await refreshed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            migrationService.Verify(
                service => service.RefreshAsync(It.IsAny<CancellationToken>()),
                Times.Once);
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
        int refreshCount = 0;
        Mock<IDatabaseMigrationService> migrationService = new();
        migrationService
            .Setup(service => service.RefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (Interlocked.Increment(ref refreshCount) >= 2)
                    refreshedTwice.TrySetResult();

                return Task.CompletedTask;
            });
        DatabaseMigrationRefreshService worker = CreateWorker(migrationService.Object, TimeSpan.FromMilliseconds(10));

        await worker.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await refreshedTwice.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(Volatile.Read(ref refreshCount) >= 2);
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
        Mock<IDatabaseMigrationService> migrationService = new();
        migrationService
            .Setup(service => service.RefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken cancellationToken) =>
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
        DatabaseMigrationRefreshService worker = CreateWorker(migrationService.Object, TimeSpan.FromHours(1));

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await refreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);

        await refreshCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshFails_LogsAndContinuesRefreshing()
    {
        TaskCompletionSource secondRefresh = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int refreshCount = 0;
        Mock<IDatabaseMigrationService> migrationService = new();
        migrationService.SetupGet(service => service.CurrentVersion).Returns(2);
        migrationService
            .Setup(service => service.RefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                int count = Interlocked.Increment(ref refreshCount);
                if (count == 1)
                    throw new InvalidOperationException("Refresh failed.");

                secondRefresh.TrySetResult();
                return Task.CompletedTask;
            });
        TestLogger<DatabaseMigrationRefreshService> logger = new();
        DatabaseMigrationRefreshService worker = new(
            migrationService.Object,
            logger,
            TimeSpan.FromMilliseconds(10));

        await worker.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await secondRefresh.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            Assert.Equal(2, migrationService.Object.CurrentVersion);
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

}
