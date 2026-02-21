/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using System;
using System.Threading.Tasks;

namespace Spark.Engine.Service;

/// <summary>
/// An <see cref="IServiceListener"/> that enqueues FHIR resource write events onto the
/// <see cref="IIndexQueue"/> for asynchronous processing by <c>IndexWorker</c>.
/// Register this in place of the default <c>SearchService</c> listener to switch from
/// synchronous to background indexing.
/// </summary>
public class IndexQueueEnqueueListener : IServiceListener
{
    private readonly IIndexQueue _indexQueue;

    public IndexQueueEnqueueListener(IIndexQueue indexQueue)
    {
        _indexQueue = indexQueue;
    }

    public Task InformAsync(Uri location, Entry interaction)
    {
        return _indexQueue.EnqueueAsync(interaction);
    }
}
