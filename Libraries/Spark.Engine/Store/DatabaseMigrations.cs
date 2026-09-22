/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Collections.Generic;

namespace Spark.Engine.Store;

public static class DatabaseMigrations
{
    public static readonly DatabaseMigration StructuredStringTokenIndex = new()
    {
        Version = 1,
        Name = "structured-string-token-index"
    };

    public static readonly DatabaseMigration TokenQuantityAndReferenceArrayIndex = new()
    {
        Version = 2,
        Name = "token-quantity-and-reference-array-index"
    };

    public static IReadOnlyList<DatabaseMigration> All { get; } =
    [
        StructuredStringTokenIndex,
        TokenQuantityAndReferenceArrayIndex
    ];
}
