/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Engine.Store;

public class SnapshotStoreSettings
{
    /// <summary>
    /// How long the snapshot of a search is kept.
    /// </summary>
    public int RetentionSeconds { get; set; } = 3600;
}
