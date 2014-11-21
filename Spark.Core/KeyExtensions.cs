/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public static class Key
    {
        public const string CID = "cid";

        public static Uri NewCID()
        {
            string s = string.Format("{0}:{1}", CID, Guid.NewGuid());
            return new Uri(s);
        }

        public static Uri NewUrn()
        {
            return new Uri("urn:guid:" + Guid.NewGuid());
        }

        public static Uri NewUuid()
        {
            return new Uri("urn:uuid:" + Guid.NewGuid().ToString());
        }

        public static bool IsCID(Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                return (uri.Scheme.ToLower() == CID);
            }
            else
            {
                return false;
            }
        }

        public static bool IsHttpScheme(Uri uri)
        {
            if (uri != null)
            {
                if (uri.IsAbsoluteUri)
                {
                    return (uri.Scheme == Uri.UriSchemeHttp) || (uri.Scheme == Uri.UriSchemeHttps);
                }

            }
            return false;
        }

        public static bool IsValidExternalKey(Uri key)
        {
            return (key != null) && (Key.IsCID(key) || Key.IsHttpScheme(key));
        }

        public static bool IsValidLocalKey(Uri key, string resourcetype)
        {
            if (key == null) return false;

            string s = key.ToString();
            string[] segments = s.Split('/');

            if (segments.Count() != 2) return false;

            bool valid_resource = (resourcetype == segments[0]);
            bool valid_id = Id.IsValidValue(segments[1]);
            return valid_resource && valid_id;
        }

        public static Uri GetOperationpath(this Uri uri)
        {
            var identity = new ResourceIdentity(uri);
            Uri key = identity.OperationPath;
            return key;
        }
        /*
        public Uri Localize(Uri external)
        {
            var identity = new ResourceIdentity(external);
            Uri local = identity.OperationPath;
            this.Map(external, local);
            return local;
        }
        */

        public static Uri HistoryKeyFor(this IGenerator generator, Uri key)
        {
            var identity = new ResourceIdentity(key);
            string vid = generator.NextHistoryKey(identity.Collection);
            Uri result = identity.WithVersion(vid);
            return result;

        }

        public static Uri FromLocation(Uri location)
        {
            var identity = new ResourceIdentity(location);
            Uri result = identity.OperationPath;
            return result;
        }


        public static bool HasValidLocalKey(BundleEntry entry)
        {
            string type = entry.GetResourceTypeName();
            Uri id = entry.Id;
            return Key.IsValidLocalKey(id, type);

        }

        public static bool KeyNeedsRemap(this Localhost localhost, Uri key)
        {
            // relative uri's are always local to our service. 
            if (!key.IsAbsoluteUri) return false;

            // cid: urls signal placeholder id's and so are never to be considered as true identities
            if (IsCID(key)) return true;

            // Check whether the uri starts with a well-known service path that shares our ID space.
            // Or is an external path that we don't share id's with
            return localhost.HasEndpointFor(key);
        }

        public static string GetResourceTypeName(this BundleEntry entry)
        {
            ResourceIdentity identity;

            if (entry is ResourceEntry)
            {
                return (entry as ResourceEntry).Resource.GetCollectionName();
            }
            else if (Key.IsHttpScheme(entry.Id))
            {
                identity = new ResourceIdentity(entry.Id);
                if (identity.Collection != null)
                    return identity.Collection;
            }
            else if (Key.IsHttpScheme(entry.SelfLink))
            {
                identity = new ResourceIdentity(entry.SelfLink);

                if (identity.Collection != null)
                    return identity.Collection;
            }
            else if (entry.SelfLink != null)
            {
                string[] segments = entry.SelfLink.ToString().Split('/');
                return segments[0];
            }


            throw new InvalidOperationException("Encountered a entry without an id, self-link or content that indicates the resource's type");
        }



    }
}
