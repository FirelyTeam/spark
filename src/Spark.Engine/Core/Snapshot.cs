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

        public static Snapshot Create(Bundle.BundleType type, Uri selflink, IEnumerable<string> keys, string sortby, int? count, IList<string> includes)
        {
            Snapshot snapshot = new Snapshot();
            snapshot.Type = type;
            snapshot.Id = Snapshot.CreateKey();
            snapshot.WhenCreated = DateTimeOffset.UtcNow;
            snapshot.FeedSelfLink = selflink.ToString();

            snapshot.Includes = includes;
            snapshot.Keys = keys;
            snapshot.Count = keys.Count();
            snapshot.CountParam = count;

            snapshot.SortBy = sortby;
            return snapshot;
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