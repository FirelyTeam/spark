/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spark.Service
{
    public class ResourceExporter
    {
        private Uri _endpoint;
        public ResourceExporter(Uri endpoint)
        {
            this._endpoint = endpoint;
        }

        public void EnsureAbsoluteUris(Bundle bundle)
        {
            bundle.Id = makeAbsolute(bundle.Id);

            foreach (var link in bundle.Links)
                link.Uri = makeAbsolute(link.Uri);

            foreach (BundleEntry be in bundle.Entries)
                EnsureAbsoluteUris(be);

            bundle.Links.Base = _endpoint;
        }

        public void EnsureAbsoluteUris(BundleEntry entry)
        {
            if (!entry.Id.IsAbsoluteUri)
                entry.Id = makeAbsolute(entry.Id);
            if (!entry.Links.SelfLink.IsAbsoluteUri)
                entry.Links.SelfLink = makeAbsolute(entry.Links.SelfLink);
        }
        
        private Uri makeAbsolute(Uri uri)
        {
            if (uri == null) return null;
            string s = uri.ToString();

            if (!uri.IsAbsoluteUri)
                return new RestUrl(_endpoint).AddPath(s).Uri;

            else
                return uri;
        }

        public void RemoveBodyFromEntries(IEnumerable<BundleEntry> entries)
        {
            foreach (BundleEntry entry in entries)
            {
                if (entry is ResourceEntry)
                {
                    (entry as ResourceEntry).Resource = null;
                }
            }
        }
    }
}