/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spark.Engine.Store.Interfaces;

public interface IIndexStore2 : IIndexStore
{
    Task<IReadOnlyList<IndexStoreWriteFailure>> SaveBatchAsync(IReadOnlyList<IndexValue> indexValues);
}

public sealed record IndexStoreWriteFailure
{
    public required int IndexValueIndex { get; init; }
    public required Exception Exception { get; init; }
}
