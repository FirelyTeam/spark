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

        public string SnapshotKey { get; set; }
        public IEnumerable<Uri> Keys { get; set; }
        public string FeedTitle { get; set; }
        public string FeedSelfLink { get; set; }
        public int Count { get; set; }
        public DateTimeOffset WhenCreated;
        public ICollection<string> Includes;

        public static Snapshot Create(string title, Uri selflink, IEnumerable<Uri> keys, IEnumerable<string> includes = null )
        {
            Snapshot snapshot = new Snapshot();
            snapshot.SnapshotKey = CreateKey();
            snapshot.WhenCreated = DateTimeOffset.UtcNow;
            snapshot.FeedTitle = title;
            snapshot.FeedSelfLink = selflink.ToString(); 
            snapshot.Includes = includes.ToList();
            snapshot.Keys = keys;
            snapshot.Count = keys.Count();
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