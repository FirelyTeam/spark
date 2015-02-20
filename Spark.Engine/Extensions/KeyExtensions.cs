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
   
    public static class KeyExtensions
    {
        public static Key ExtractKey(this Resource resource)
        {
            string _base = (resource.ResourceBase != null) ? resource.ResourceBase.ToString() : null;
            Key key = new Key(_base, resource.TypeName, resource.Id, resource.VersionId);
            return key;
        }

        public static Key ExtractKey(this Bundle.BundleEntryComponent entry)
        {
            if (entry.Deleted != null)
            {
                return Key.CreateLocal(entry.Deleted.Type, entry.Deleted.ResourceId, entry.Deleted.VersionId);
            }
            else
            {
                return ExtractKey(entry.Resource);
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
          
        // Important! This is the core logic for the difference between an internal and external key.
        public static bool IsForeign(this Key key)
        {
            if (key.Base == null) return false;
            
            return Localhost.IsEndpointOf(key.Base);
        }

        public static bool IsTemporary(this Key key)
        {
            if (key.ResourceId != null)
            {
                return UriHelper.IsTemporaryUri(key.ResourceId);
            }
            else return false;
        }

        public static bool IsInternal(this Key key)
        {
            return !(key.IsTemporary() || key.IsForeign());
        }

        

        

    }
}
