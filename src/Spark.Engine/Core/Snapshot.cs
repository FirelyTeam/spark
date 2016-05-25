/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Extensions;
using Hl7.Fhir.Rest;

namespace Spark.Engine.Core
{
    public class Snapshot
    {
        public const int NOCOUNT = -1;
        public const int MAX_PAGE_SIZE = 100;


        public string Id { get; set; }
        public Bundle.BundleType Type { get; set; }
        public IEnumerable<string> Keys { get; set; }
        //public string FeedTitle { get; set; }
        public string FeedSelfLink { get; set; }
        public int Count { get; set; }
        public int? CountParam { get; set; }
        public DateTimeOffset WhenCreated;
        public string SortBy { get; set; }
        public ICollection<string> Includes;
        public ICollection<string> ReverseIncludes;

        public static Snapshot Create(Bundle.BundleType type, Uri selflink, IEnumerable<string> keys, string sortby, int? count, IList<string> includes, IList<string> reverseIncludes)
        {
            Snapshot snapshot = new Snapshot();
            snapshot.Type = type;
            snapshot.Id = Snapshot.CreateKey();
            snapshot.WhenCreated = DateTimeOffset.UtcNow;
            snapshot.FeedSelfLink = selflink.ToString();

            snapshot.Includes = includes;
            snapshot.ReverseIncludes = reverseIncludes;
            snapshot.Keys = keys;
            snapshot.Count = keys.Count();
            snapshot.CountParam = NormalizeCount(count);

            snapshot.SortBy = sortby;
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