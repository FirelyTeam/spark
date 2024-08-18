/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly)
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Extensions;

namespace Spark.Engine.Core
{
    public class Snapshot
    {
        public const int NOCOUNT = -1;
        public const int MAX_PAGE_SIZE = 100;

        public string Id { get; set; }
        public Bundle.BundleType Type { get; set; }
        public IReadOnlyList<string> Keys { get; set; }
        public string FeedSelfLink { get; set; }
        public int Count { get; set; }
        public int? CountParam { get; set; }
        public DateTimeOffset WhenCreated;
        public string SortBy { get; set; }
        public IReadOnlyList<string> Includes;
        public IReadOnlyList<string> ReverseIncludes;
        public IReadOnlyList<string> Elements;

        public static Snapshot Create(Bundle.BundleType type, Uri selflink, IReadOnlyList<string> keys, string sortby, int? count, IReadOnlyList<string> includes, IReadOnlyList<string> reverseIncludes, IReadOnlyList<string> elements)
        {
            Snapshot snapshot = new Snapshot
            {
                Type = type,
                Id = CreateKey(),
                WhenCreated = DateTimeOffset.UtcNow,
                FeedSelfLink = selflink.ToString(),

                Includes = includes,
                ReverseIncludes = reverseIncludes,
                Elements = elements,
                Keys = keys,
                Count = keys.Count(),
                CountParam = NormalizeCount(count),

                SortBy = sortby
            };
            return snapshot;
        }

        private static int? NormalizeCount(int? count)
        {
            if (count.HasValue)
            {
                return Math.Min(count.Value, MAX_PAGE_SIZE);
            }
            return count;
        }

        public static string CreateKey()
        {
            return Guid.NewGuid().ToString();
        }

        public bool InRange(int index)
        {
            if (index == 0 && Keys.Count() == 0)
                return true;

            int last = Keys.Count()-1;
            return (index > 0 || index <= last);
        }
    }

    public static class SnapshotExtensions 
    {
        [Obsolete("Method will be removed in a future version")]
        public static IEnumerable<string> Keys(this Bundle bundle)
        {
            return bundle.GetResources().Keys();
        }

        public static IEnumerable<string> Keys(this IEnumerable<Resource> resources)
        {
            return resources.Select(e => e.VersionId);
        }
    }
}