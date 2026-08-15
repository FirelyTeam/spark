/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Engine.Store;

public static class DatabaseMigrations
{
    public static readonly DatabaseMigration StructuredStringTokenIndex = new()
    {
        Version = 1,
        Name = "structured-string-token-index"
    };
}
