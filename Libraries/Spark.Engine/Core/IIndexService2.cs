/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Store;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spark.Engine.Core;

public interface IIndexService2 : IIndexService
{
    Task<IReadOnlyList<IndexBatchFailure>> ProcessBatchAsync(IReadOnlyList<Entry> entries);
}

public sealed record IndexBatchFailure
{
    public required Entry Entry { get; init; }
    public required Exception Exception { get; init; }
}
