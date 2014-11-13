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
        public static IEnumerable<Uri> SelfLinks(this Bundle bundle)
        {
            return bundle.Entries.Select(entry => entry.Links.SelfLink);
        }

        public static IEnumerable<Uri> GetReferences(this BundleEntry entry, string include)
        {
            Resource resource = (entry as ResourceEntry).Resource;
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
            foreach (BundleEntry entry in bundle.Entries)
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

        public static void AddRange(this Bundle bundle, IEnumerable<BundleEntry> entries)
        {
            foreach (BundleEntry entry in entries)
            {
                bundle.Entries.Add(entry);
            }
        }

        public static string TypeName(this BundleEntry entry)
        {
            if (entry is DeletedEntry)
            {
                return "DeletedEntry";
            }
            else if (entry is ResourceEntry)
            {
                return "ResourceEntry";
            }
            else
            {
                throw new ArgumentException("Unsupported BundleEntry type: " + entry.GetType().Name);
            }
        }

        public static void OverloadKey(this BundleEntry entry, Uri key)
        {
            if (entry.Id != key)
            {
                
                Uri old = entry.Id;
                entry.Id = key;

                if (!entry.Links.Any(u => u.Uri == old))
                {
                    entry.Links.Alternate = old;
                }
            }
        }

        public static string GetResourceTypeName(this BundleEntry entry)
        {
            ResourceIdentity identity;

            if (entry is ResourceEntry)
                return (entry as ResourceEntry).Resource.GetCollectionName();

            if (entry.Id.Scheme == Uri.UriSchemeHttp)
            {
                identity = new ResourceIdentity(entry.Id);
                if (identity.Collection != null)
                    return identity.Collection;
            }

            if (entry.SelfLink != null && entry.SelfLink.Scheme == Uri.UriSchemeHttp)
            {
                identity = new ResourceIdentity(entry.SelfLink);

                if (identity.Collection != null)
                    return identity.Collection;
            }

            throw new InvalidOperationException("Encountered a entry without an id, self-link or content that indicates the resource's type");
        }

    }
}