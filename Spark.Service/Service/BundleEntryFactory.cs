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

namespace Spark.Service
{
    internal static class BundleEntryFactory
    {
        internal static DeletedEntry CreateNewDeletedEntry( Uri id )
        {
           return new DeletedEntry() { Id = id, When = DateTimeOffset.Now };
        }

        internal static Bundle CreateBundleWithEntries(string title, Uri feedUri, string author, string authorUri, IEnumerable<BundleEntry> entries = null)
        {
            Bundle responseBundle = new Bundle();
            responseBundle.Title = title;
            responseBundle.Id = new Uri("urn:uuid:" + Guid.NewGuid().ToString());
            responseBundle.AuthorName = author;
            responseBundle.AuthorUri = authorUri;

            responseBundle.Links = new UriLinkList();
            responseBundle.Links.SelfLink = feedUri;
            responseBundle.LastUpdated = DateTimeOffset.Now;

            if (entries != null)
            {
                foreach (BundleEntry entry in entries)
                {
                    responseBundle.Entries.Add(entry);
                }
            }      
            return responseBundle;
        }

        internal static ResourceEntry CreateFromResource(Resource resource, Uri id, DateTimeOffset updated, string title = null)
        {
            var result = ResourceEntry.Create(resource);
            initializeResourceEntry(result, id, updated, title);

            result.Resource = resource;

            return result;
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

        private static void initializeResourceEntry(ResourceEntry member, Uri id, DateTimeOffset updated, string title)
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

    }
}
