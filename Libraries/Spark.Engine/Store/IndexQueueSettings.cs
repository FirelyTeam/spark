/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Engine.Store;

public class IndexQueueSettings
{
    /// <summary>
    /// How long a claimed entry may remain in the processing state before being
    /// considered stale and reclaimed by another worker.
    /// </summary>
    public TimeSpan LeaseTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum number of processing attempts before an entry is moved to failed.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// How long the IndexWorker sleeps between polling cycles when the queue is empty.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(20);
}
