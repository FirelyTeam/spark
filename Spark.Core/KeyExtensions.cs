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
    public struct Key
    {
        public string TypeName;
        public string ResourceId;
        public string VersionId;

        public Key(string type, string resourceid)
        {
            this.TypeName = type;
            this.ResourceId = resourceid;
            this.VersionId = null;
        }

        public Key(string type, string resourceid, string versionid)
        {
            this.TypeName = type;
            this.ResourceId = resourceid;
            this.VersionId = versionid;
        }

        public override string ToString()
        {
            string s = string.Format("{0}/{1}", TypeName, ResourceId);
            if (VersionId != null)
            {
                s += string.Format("/{0}/{1}", RestOperation.HISTORY, VersionId);
            }
            return s;
        }

        public Key WithoutVersion()
        {
            Key key = this;
            key.VersionId = null;
            return key;
        }

        public static Key Null
        { 
            get
            {
                return default(Key);
            }
        }

        public bool HasVersion
        {
            get
            {
                return string.IsNullOrEmpty(VersionId);
            }
        }
    }

    public static class KeyExtensions
    {
        public static Key GetKey(this Resource resource)
        {
            Key key = new Key(resource.TypeName, resource.Id, resource.VersionId);
            return key;
        }

        public static Key GetKey(this Bundle.BundleEntryComponent entry)
        {
            if (entry.Deleted != null)
            {
                return new Key(entry.Deleted.TypeName, entry.Deleted.ResourceId, entry.Deleted.VersionId);
            }
            else
            {
                return entry.Resource.GetKey();
            }
        }

        public static Uri ToRelativeUri(this Key key)
        {
            return new Uri(key.ToString());
        }

        public static Uri ToUri(this Key key, Uri endpoint)
        {
            string _base = endpoint.ToString().TrimEnd('/');
            string s = string.Format("{0}/{1}", _base, key);
            return new Uri(s);
        }

    }



    public static class KeyHelper
    {
        public const string CID = "cid";

        public static string NewCID()
        {
            return string.Format("{0}:{1}", CID, Guid.NewGuid());
            
        }

        public static Uri NewUrn()
        {
            return new Uri("urn:guid:" + Guid.NewGuid());
        }

        public static Uri NewUuid()
        {
            return new Uri("urn:uuid:" + Guid.NewGuid().ToString());
        }

        public static bool IsCID(Key key)
        {
            // todo: DSTU2
            // dit was eerst een Uri. Geen idee hoe we de Id nu gaan controleren
            /*
            if (uri.IsAbsoluteUri)
            {
                return (uri.Scheme.ToLower() == CID);
            }
            else
            {
                return false;
            }
            */
            return false;
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

        public static bool IsValidExternalKey(Key key)
        {
            // todo: DSTU2 - httpscheme hoeven we niet meer te controleren?
            return KeyHelper.IsCID(key); //|| KeyHelper.IsHttpScheme(key));
        }

        //public static bool IsValidLocalKey(string key, string resourcetype)
        //{
        //    if (key == null) return false;

        //    string s = key.ToString();
        //    string[] segments = s.Split('/');

        //    if (segments.Count() != 2) return false;

        //    bool valid_resource = (resourcetype == segments[0]);
        //    bool valid_id = Id.IsValidValue(segments[1]);
        //    return valid_resource && valid_id;
        //}

        public static Uri GetOperationpath(this Uri uri)
        {
            var identity = new ResourceIdentity(uri);
            // todo: DSTU2 // function doesn't exist anymore
            //Uri key = identity.Operationpath;
            //return key;
            return null;
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
            string vid = generator.NextHistoryKey(identity.ResourceType);
            Uri result = identity.WithVersion(vid);
            return result;

        }

        public static Uri FromLocation(Uri location)
        {
            var identity = new ResourceIdentity(location);
            // todo: DSTU2
            //Uri result = identity.GetOperationPath;
            //return result;
            return null;
        }


        //public static bool HasValidLocalKey(Resource resource)
        //{
        //    string type = resource.TypeName;
        //    string id = resource.Id;
        //    return KeyHelper.IsValidLocalKey(id, type);

        //}

        /*
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
        */

        // todo: DSTU2. I removed it because the Api can now provide the type name.
        /*
         
         public static string GetResourceTypeName(this Resource entry)
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
        */


    }
}
