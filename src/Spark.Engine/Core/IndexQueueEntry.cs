/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Engine.Core;

public class IndexQueueEntry
{
    public string Id { get; set; }
    public Entry Entry { get; set; }
    public int Attempts { get; set; }
    public string LastError { get; set; }
    public DateTime EnqueuedAt { get; set; }
}
