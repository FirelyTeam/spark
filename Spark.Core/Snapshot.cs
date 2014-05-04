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
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using Spark.Core;

namespace Spark.Core
{
    public class Snapshot
    {
        public const int NOCOUNT = -1;

        public string Id { get; set; }
        public IEnumerable<Uri> Contents { get; set; }
        public string FeedTitle { get; set; }
        public string FeedSelfLink { get; set; }
        public int MatchCount { get; set; }
        public ICollection<string> Includes;

        public static Snapshot TakeSnapshotFromBundle(Bundle bundle)
        {
            // Is Snapshot not a type of bundle???
            Snapshot snapshot = new Snapshot();
            snapshot.FeedTitle = bundle.Title;
            snapshot.Id = Guid.NewGuid().ToString();
            snapshot.FeedSelfLink = bundle.Links.SelfLink.ToString();
            snapshot.Contents = bundle.SelfLinks();
            snapshot.MatchCount = snapshot.Contents.Count();
            return snapshot;
        }

        public static Snapshot Create(string title, Uri selflink, ICollection<string> includes, IEnumerable<Uri> keys, int matchCount)
        {
            Snapshot snapshot = new Snapshot();
            snapshot.Id = Guid.NewGuid().ToString();
            snapshot.FeedTitle = title;
            snapshot.FeedSelfLink = selflink.ToString(); // todo: moet FeedSelfLink geen Uri zijn? - yes. possible.
            snapshot.Includes = includes;
            snapshot.Contents = keys;
            snapshot.MatchCount = (matchCount == 0) ? keys.Count() : matchCount;
            return snapshot;
        }

        public static Snapshot Create(string title, Uri selflink, ICollection<string> includes, IEnumerable<BundleEntry> entries, int matchCount)
        {
            return Create(title, selflink, includes, entries.Keys(), matchCount);
        }

        public bool InRange(int index)
        {
            if (index == 0 && Contents.Count() == 0)
                return true;

            int last = Contents.Count()-1;
            return (index > 0 || index <= last);
        }
    }


    public static class SnapshotExtensions 
    {
        public static IEnumerable<Uri> Keys(this Bundle bundle)
        {
            return bundle.Entries.Keys();
        }
        public static IEnumerable<Uri> Keys(this IEnumerable<BundleEntry> entries)
        {
            return entries.Select(e => e.Links.SelfLink);
        }
    }
}