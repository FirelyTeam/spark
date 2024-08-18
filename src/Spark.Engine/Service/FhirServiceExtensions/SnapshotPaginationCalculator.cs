/* 
 * Copyright (c) 2016-2018, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class SnapshotPaginationCalculator : ISnapshotPaginationCalculator
    {
        public const int DEFAULT_PAGE_SIZE = 20;

        public IEnumerable<IKey> GetKeysForPage(Snapshot snapshot, int? start = null)
        {
            IEnumerable<string> keysInBundle = snapshot.Keys;
            if (start.HasValue)
            {
                keysInBundle = keysInBundle.Skip(start.Value);
            }
            return keysInBundle.Take(snapshot.CountParam ?? DEFAULT_PAGE_SIZE).Select(k => (IKey)Key.ParseOperationPath(k)).ToList();
        }

        public int GetIndexForLastPage(Snapshot snapshot)
        {
            int countParam = snapshot.CountParam ?? DEFAULT_PAGE_SIZE;
            if (snapshot.Count <= countParam)
                return 0;

            int numberOfPages = snapshot.Count/countParam;
            int lastPageIndex = (snapshot.Count%countParam == 0) ? numberOfPages - 1 : numberOfPages;
            return lastPageIndex*countParam;
        }

        public int? GetIndexForNextPage(Snapshot snapshot, int? start = null)
        {
            int countParam = snapshot.CountParam ?? DEFAULT_PAGE_SIZE;

            if (((start ?? 0) + countParam) >= snapshot.Count)
                return null;
            return (start ?? 0) + countParam;
        }

        public int? GetIndexForPreviousPage(Snapshot snapshot, int? start = null)
        {
            int countParam = snapshot.CountParam ?? DEFAULT_PAGE_SIZE;
            if (start.HasValue == false || start.Value == 0)
                return null;
            return Math.Max(0, start.Value - countParam);
        }
    }
}