/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Threading.Tasks;

namespace Spark.Engine.Service.FhirServiceExtensions;

public interface IPagingService2 : IPagingService
{
    Task<ISnapshotPagination> StartPaginationAsync(string snapshotKey, int offset);
}
