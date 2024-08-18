/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Collections.Generic;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ISnapshotPaginationCalculator
    {
        IEnumerable<IKey> GetKeysForPage(Snapshot snapshot, int? start = null);
        int GetIndexForLastPage(Snapshot snapshot);
        int? GetIndexForNextPage(Snapshot snapshot, int? start = null);
        int? GetIndexForPreviousPage(Snapshot snapshot, int? start = null);
    }
}