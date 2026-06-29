/*
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Service.FhirServiceExtensions;

public class PagingService : IPagingService2
{
    private readonly ISnapshotStore _snapshotstore;
    private readonly ISnapshotStore2 _snapshotstore2;
    private readonly ISnapshotPaginationProvider _paginationProvider;

    public PagingService(ISnapshotStore snapshotstore, ISnapshotPaginationProvider paginationProvider)
    {
        _snapshotstore = snapshotstore;
        _snapshotstore2 = snapshotstore as ISnapshotStore2;
        _paginationProvider = paginationProvider;
    }

    public async Task<ISnapshotPagination> StartPaginationAsync(Snapshot snapshot)
    {
        if (_snapshotstore != null)
        {
            await _snapshotstore.AddSnapshotAsync(snapshot).ConfigureAwait(false);
        }
        else
        {
            snapshot.Id = null;
        }

        return _paginationProvider.StartPagination(snapshot);
    }
    public async Task<ISnapshotPagination> StartPaginationAsync(string snapshotkey)
    {
        return await StartPaginationAsync(snapshotkey, 0).ConfigureAwait(false);
    }

    public async Task<ISnapshotPagination> StartPaginationAsync(string snapshotkey, int offset)
    {
        if (_snapshotstore == null)
        {
            throw new NotSupportedException("Stateful pagination is not currently supported.");
        }

        var snapshot = _snapshotstore2 == null
            ? await _snapshotstore.GetSnapshotAsync(snapshotkey).ConfigureAwait(false)
            : await _snapshotstore2.GetSnapshotAsync(snapshotkey, offset).ConfigureAwait(false);

        return _paginationProvider.StartPagination(snapshot);
    }
}
