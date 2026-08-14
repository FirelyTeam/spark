/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Threading;
using System.Threading.Tasks;

namespace Spark.Engine.Store.Interfaces;

public interface IDatabaseMigrationService
{
    int CurrentVersion { get; }

    bool IsApplied(int version);

    Task RefreshAsync(CancellationToken cancellationToken = default);

    Task RecordCompletedAsync(
        DatabaseMigration migration,
        CancellationToken cancellationToken = default);
}
