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

namespace Spark.Core
{
    public static class FhirModelExtensions
    {

        public static Uri ConstructSelfLink(string baseuri, Resource resource)
        {

            // you must assume the resource has a verion id, otherwise a selflink is not possible
            string s = baseuri + "/" + resource.TypeName + "/" + resource.Id;
            if (resource.HasVersionId)
            {
                s += "/_history/" + resource.VersionId;
            }
            return new Uri(s);
        }

        public static IEnumerable<Uri> SelfLinks(this Bundle bundle)
        {
            // API: ewout This could probably be resolved through the api? / Is this still needed in DSTU2
            // antwoord: je kunt nu Resource.ResourceIdentity aanroepen.
            return bundle.GetResources().Select(r => ConstructSelfLink(bundle.Base, r));
        }

        public static IEnumerable<Uri> GetReferences(this Resource resource, string include)
        {
            ElementQuery query = new ElementQuery(include);
            var list = new List<Uri>();

            query.Visit(resource, element =>
            {
                if (element is ResourceReference)
                {
                    Uri uri = (element as ResourceReference).Url;
                    if (uri != null) list.Add(uri);
                }
            });
            return list.Where(u => u != null);
        }

        public static IEnumerable<Uri> GetReferences(this Bundle bundle, string include)
        {
            foreach (Resource entry in bundle.GetResources())
            {
                IEnumerable<Uri> list = GetReferences(entry, include);
                foreach (Uri value in list)
                {
                    if (value != null)
                        yield return value;
                }
            }
        }

        public static IEnumerable<Uri> GetReferences(this Bundle bundle, IEnumerable<string> includes)
        {
            return includes.SelectMany(include => GetReferences(bundle, include));
        }

        public static void AddRange(this Bundle bundle, IEnumerable<Resource> resources)
        {
            foreach (Resource resource in resources)
            {
                var entry = new Bundle.BundleEntryComponent();
                entry.Resource = resource;
                entry.Base = bundle.Base;
                bundle.Entry.Add(entry); 
            }
        }

    }

}