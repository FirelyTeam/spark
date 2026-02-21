/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Service;
using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Test.Service;

public class IndexWorkerTests
{
    private readonly Mock<IIndexQueue> _indexQueueMock = new();
    private readonly Mock<IIndexService> _indexServiceMock = new();
    private readonly IndexQueueSettings _settings = new() { PollInterval = TimeSpan.FromMilliseconds(1) };

    private IndexWorker CreateWorker() =>
        new(_indexQueueMock.Object, _indexServiceMock.Object,
            NullLogger<IndexWorker>.Instance, _settings);

    /// <summary>
    /// Configures ClaimNextAsync to return <paramref name="first"/> on the first call,
    /// then block indefinitely (until canceled) on subsequent calls.
    /// </summary>
    private void SetupClaimSequence(IndexQueueEntry first)
    {
        int calls = 0;
        _indexQueueMock.Setup(indexQueue => indexQueue.ClaimNextAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                    return first;
                await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
                return null;
            });
    }

    [Fact]
    public async Task ExecuteAsync_WhenEntryAvailable_ProcessesAndAcknowledges()
    {
        var entry = new IndexQueueEntry { Id = "e1", Entry = Entry.POST(new Key("http://localhost/", "Patient", "p1", null), new Patient()) };
        SetupClaimSequence(entry);
        
        // Signal when AcknowledgeAsync is called so the test doesn't rely on timing
        var ackSignal = new TaskCompletionSource();
        _indexQueueMock.Setup(indexQueue => indexQueue.AcknowledgeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => ackSignal.TrySetResult());

        var worker = CreateWorker();
        await worker.StartAsync(CancellationToken.None);
        await ackSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        _indexServiceMock.Verify(indexService => indexService.ProcessAsync(entry.Entry), Times.Once);
        _indexQueueMock.Verify(indexQueue => indexQueue.AcknowledgeAsync("e1", It.IsAny<CancellationToken>()), Times.Once);
        _indexQueueMock.Verify(indexQueue => indexQueue.NackAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProcessingFails_CallsNackWithEntryIdAndError()
    {
        var entry = new IndexQueueEntry { Id = "e2", Entry = Entry.POST(new Key("http://localhost/", "Patient", "p2", null), new Patient()) };
        SetupClaimSequence(entry);

        var processingError = new InvalidOperationException("index failure");
        _indexServiceMock.Setup(indexService => indexService.ProcessAsync(It.IsAny<Entry>())).ThrowsAsync(processingError);

        var nackSignal = new TaskCompletionSource();
        _indexQueueMock.Setup(indexQueue => indexQueue.NackAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => nackSignal.TrySetResult());

        var worker = CreateWorker();
        await worker.StartAsync(CancellationToken.None);
        await nackSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        _indexQueueMock.Verify(indexQueue => indexQueue.NackAsync("e2", "index failure", It.IsAny<CancellationToken>()), Times.Once);
        _indexQueueMock.Verify(indexQueue => indexQueue.AcknowledgeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenQueueIsEmpty_DoesNotCallIndexService()
    {
        int calls = 0;
        var emptyQueueSignal = new TaskCompletionSource();
        _indexQueueMock.Setup(indexQueue => indexQueue.ClaimNextAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    emptyQueueSignal.TrySetResult();
                    return null;
                }
                await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
                return null;
            });

        var worker = CreateWorker();
        await worker.StartAsync(CancellationToken.None);
        await emptyQueueSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        _indexQueueMock.Verify(indexQueue => indexQueue.ClaimNextAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _indexServiceMock.Verify(indexService => indexService.ProcessAsync(It.IsAny<Entry>()), Times.Never);
        _indexQueueMock.Verify(indexQueue => indexQueue.AcknowledgeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OnCancellation_ExitsWithoutThrowing()
    {
        _indexQueueMock.Setup(q => q.ClaimNextAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
                return null;
            });

        var worker = CreateWorker();
        await worker.StartAsync(CancellationToken.None);
        var exception = await Record.ExceptionAsync(() => worker.StopAsync(CancellationToken.None));
        Assert.Null(exception);
    }
}
