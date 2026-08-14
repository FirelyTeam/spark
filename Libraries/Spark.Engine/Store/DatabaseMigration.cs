/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Engine.Store;

public sealed record DatabaseMigration
{
    public required int Version { get; init; }

    public required string Name { get; init; }
}
