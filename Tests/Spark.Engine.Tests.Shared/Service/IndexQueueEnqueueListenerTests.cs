/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Threading;
using Hl7.Fhir.Model;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Service;
using Spark.Engine.Store.Interfaces;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Tests.Service;

public class IndexQueueEnqueueListenerTests
{
    [Fact]
    public async Task InformAsync_ForwardsEntryToIndexQueue()
    {
        var mockQueue = new Mock<IIndexQueue>();
        var entry = Entry.POST(new Key("http://localhost/", "Patient", "p1", null), new Patient { Id = "p1" });
        var listener = new IndexQueueEnqueueListener(mockQueue.Object);

        await listener.InformAsync(new Uri("http://localhost/Patient/p1"), entry);

        mockQueue.Verify(q => q.EnqueueAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
    }
}
