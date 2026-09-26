/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;
using System;

namespace Spark.Store.MongoDB.Search;

[Flags]
internal enum SearchIndexMigrationState
{
    None = 0,
    StructuredStringTokenIndex = 1 << 0,
    TokenQuantityAndReferenceArrayIndex = 1 << 1
}

internal static class SearchIndexMigrationStateExtensions
{
    internal static SearchIndexMigrationState GetSearchIndexMigrationState(
        this IDatabaseMigrationService migrationService)
    {
        ArgumentNullException.ThrowIfNull(migrationService);

        SearchIndexMigrationState state = SearchIndexMigrationState.None;

        if (migrationService.IsApplied(DatabaseMigrations.StructuredStringTokenIndex.Version))
        {
            state |= SearchIndexMigrationState.StructuredStringTokenIndex;
        }

        if (migrationService.IsApplied(DatabaseMigrations.TokenQuantityAndReferenceArrayIndex.Version))
        {
            state |= SearchIndexMigrationState.TokenQuantityAndReferenceArrayIndex;
        }

        return state;
    }
}
