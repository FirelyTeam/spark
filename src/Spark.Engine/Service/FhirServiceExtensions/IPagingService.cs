/*
 * Copyright (c) 2016-2018, Firely (info@fire.ly)
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IPagingService : IFhirServiceExtension
    {
        Task<ISnapshotPagination> StartPaginationAsync(Snapshot snapshot);
        Task<ISnapshotPagination> StartPaginationAsync(string snapshotKey);
    }
}