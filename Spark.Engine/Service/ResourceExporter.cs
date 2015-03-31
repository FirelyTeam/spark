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
using Spark.Core;

namespace Spark.Service
{
    public class ResourceExporter
    {
        private Uri _endpoint;

        Localhost localhost;

        public ResourceExporter(Localhost localhost)
        {
            this.localhost = localhost;
        }

       
        /*
        public void Externalize(Resource entry)
        {
            ensureAbsoluteUris(entry);
        }

        public void Externalize(Bundle bundle)
        {
            ensureAbsoluteUris(bundle);

        }

        private void ensureAbsoluteUris(Bundle bundle)
        {
            bundle.Id = makeAbsolute(bundle.Id);

            foreach (var link in bundle.Links)
                link.Uri = makeAbsolute(link.Uri);

            foreach (BundleEntry be in bundle.Entries)
                ensureAbsoluteUris(be);

            bundle.Links.Base = _endpoint;
        }

        private void ensureAbsoluteUris(Resource entry)
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
        */

        /*
        public void RemoveBodyFromEntries(List<Entry> entries)
        {
            foreach (Entry entry in entries)
            {
                if (entry.IsResource())
                {
                    entry.Resource = null;
                }
            }
        }
        */
    }
}