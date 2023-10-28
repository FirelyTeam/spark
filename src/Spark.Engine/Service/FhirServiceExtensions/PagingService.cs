﻿/*
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * Copyright (c) 2021-2023, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class PagingService : IPagingService
    {
        private readonly ISnapshotStore _snapshotstore;
        private readonly ISnapshotPaginationProvider _paginationProvider;

        public PagingService(ISnapshotStore snapshotstore, ISnapshotPaginationProvider paginationProvider)
        {
            _snapshotstore = snapshotstore;
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
            if (_snapshotstore == null)
            {
                throw new NotSupportedException("Stateful pagination is not currently supported.");
            }
            return _paginationProvider.StartPagination(await _snapshotstore.GetSnapshotAsync(snapshotkey).ConfigureAwait(false));
        }
    }
}