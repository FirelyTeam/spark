/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.Extensions.Hosting;
using Spark.Engine;
using System.Threading;
using System.Threading.Tasks;

namespace Spark.Store.MongoDB;

/// <summary>
/// Makes the index that has MongoDB remove the snapshots of searches nobody is paging through any more.
/// </summary>
internal sealed class SnapshotExpiryIndexService : IHostedService
{
    private readonly StoreSettings _storeSettings;

    public SnapshotExpiryIndexService(StoreSettings storeSettings) => _storeSettings = storeSettings;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return MongoSnapshotStore.CreateExpiryIndexAsync(
            MongoDatabaseFactory.GetMongoDatabase(_storeSettings.ConnectionString),
            _storeSettings.SnapshotStore.RetentionSeconds,
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
