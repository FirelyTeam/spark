/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hl7.Fhir.Support;
using Hl7.Fhir.Model;
using System.Xml;
using Hl7.Fhir.Serialization;
using System.IO;
using System.Globalization;
using Spark.Core;

namespace Spark.Service
{
    internal static class BundleEntryFactory
    {

        internal static Bundle CreateBundleWithEntries(string title, Uri feedUri, string author, string authorUri, IEnumerable<Entry> entries = null)
        {
            Bundle bundle = new Bundle();
            // todo: DSTU2
            //bundle.Title = title;

            bundle.Id = "urn:uuid:" + Guid.NewGuid().ToString();
            //bundle.AuthorName = author;
            // bundle.AuthorUri = authorUri;

            //bundle.Links = new UriLinkList();
            //bundle.Links.SelfLink = feedUri;
            //bundle.LastUpdated = DateTimeOffset.Now;

            if (entries != null)
            {
                bundle.Append(entries);
            }

            return bundle;
        }

        internal static Entry CreateFromResource(Resource resource, DateTimeOffset updated, string title = null)
        {
            // todo: DSTU2
            // var result = ResourceEntry.Create(resource);

            Entry entry = new Entry(resource);
            entry.When = updated;
            //entry.Resource = resource;

            //initializeResourceEntry(entry, id, updated, title);
            // todo: DSTU2
            // no place for title.

            return entry;
        }

        //internal static ResourceEntry<Binary> CreateFromBinary(byte[] data, string mediaType, Uri id, DateTimeOffset updated, string title = null)
        //{
        //    var result = new ResourceEntry<Binary>();

        //    if (title == null)
        //        title = "Binary entry containing " + mediaType;

        //    initializeResourceEntry(result, id, updated, title);

        //    result.Content = new Binary() { Content = data, ContentType = mediaType };

        //    return result;
        //}


        // todo: DSTU2 -- all these things don't work anymore.
        /*
        private static void initializeResourceEntry(Entry member, DateTimeOffset updated, string title)
        {
            member.Id = id;
            member.LastUpdated = updated;
            member.Published = DateTimeOffset.Now;      // Time copied into feed
            member.Title = title;

            var userIdentity = System.Threading.Thread.CurrentPrincipal.Identity;
            member.AuthorName = userIdentity != null && !String.IsNullOrEmpty(userIdentity.Name) ?
                                        userIdentity.Name : "(unauthenticated)";

            member.AuthorUri = null; //TODO: how to get a meaningful AuthorUri?
        }
        */

    }
}
