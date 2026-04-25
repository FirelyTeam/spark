/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Threading;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces;

public interface IIndexQueue
{
    Task EnqueueAsync(Entry entry, CancellationToken cancellationToken = default);
    Task<IndexQueueEntry> ClaimNextAsync(CancellationToken cancellationToken = default);
    Task AcknowledgeAsync(string id, CancellationToken cancellationToken = default);
    Task NackAsync(string id, string error, CancellationToken cancellationToken = default);
}
