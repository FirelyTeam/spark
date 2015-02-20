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
    public static class BundleFactory
    {

        public static Bundle Create(string title, Uri feedUri, string author, string authorUri, IEnumerable<Entry> entries = null)
        {
            Bundle bundle = new Bundle();
            bundle.Id = "urn:uuid:" + Guid.NewGuid().ToString();
            bundle.Base = Localhost.Base.ToString();
            // DSTU: bundle
            // do we have all new metadata fields of bundle

            if (entries != null)
            {
                bundle.Append(entries);
            }

            return bundle;
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


        // DSTU2: meta
        // -- all these things don't work anymore.
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
