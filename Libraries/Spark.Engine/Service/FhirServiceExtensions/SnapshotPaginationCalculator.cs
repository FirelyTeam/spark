/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions;

public class SnapshotPaginationCalculator : ISnapshotPaginationCalculator
{
    public const int DEFAULT_PAGE_SIZE = Snapshot.DEFAULT_PAGE_SIZE;

    public IEnumerable<IKey> GetKeysForPage(Snapshot snapshot, int? start = null)
    {
        IEnumerable<string> keysInBundle = snapshot.Keys;
        if (start.HasValue)
        {
            keysInBundle = keysInBundle.Skip(Math.Max(0, start.Value - snapshot.StartIndex));
        }
        return keysInBundle.Take(snapshot.GetPageSize()).Select(k => (IKey)Key.ParseOperationPath(k)).ToList();
    }

    public int GetIndexForLastPage(Snapshot snapshot)
    {
        int countParam = snapshot.GetPageSize();
        if (countParam == 0)
            return 0;
        if (snapshot.Count <= countParam)
            return 0;

        int numberOfPages = snapshot.Count/countParam;
        int lastPageIndex = (snapshot.Count%countParam == 0) ? numberOfPages - 1 : numberOfPages;
        return lastPageIndex*countParam;
    }

    public int? GetIndexForNextPage(Snapshot snapshot, int? start = null)
    {
        int countParam = snapshot.GetPageSize();
        if (countParam == 0)
            return null;

        if (((start ?? 0) + countParam) >= snapshot.Count)
            return null;
        return (start ?? 0) + countParam;
    }

    public int? GetIndexForPreviousPage(Snapshot snapshot, int? start = null)
    {
        int countParam = snapshot.GetPageSize();
        if (start.HasValue == false || start.Value == 0)
            return null;
        return Math.Max(0, start.Value - countParam);
    }
}
