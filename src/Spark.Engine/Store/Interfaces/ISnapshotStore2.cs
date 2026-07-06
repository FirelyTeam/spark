/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces;

public interface ISnapshotStore2 : ISnapshotStore
{
    Task<Snapshot> GetSnapshotAsync(string snapshotId, int offset);
}
