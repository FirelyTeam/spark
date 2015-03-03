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
        public static IKey ExtractKey(this Resource resource)
        {
            string _base = (resource.ResourceBase != null) ? resource.ResourceBase.ToString() : null;
            IKey key = new Key(_base, resource.TypeName, resource.Id, resource.VersionId);
            return key;
        }

                
        public static void Apply(this IKey key, Resource resource)
        {
            resource.Id = key.ResourceId;
            resource.VersionId = key.VersionId;
        }


        public static IKey ExtractKey(this Bundle.BundleEntryComponent entry)
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

        public static Key Clone(this IKey self)
        {
            Key key = new Key(self.Base, self.TypeName, self.ResourceId, self.VersionId);
            return key;
        }

        public static Key GetLocalKey(this IKey self)
        {
            Key key = self.Clone();
            key.Base = null;
            return key;
        }

        public static Key WithBase(this IKey self, string _base)
        {
            Key key = self.Clone();
            key.Base = _base;
            return key;
        }

        public static string Path(this IKey key)
        {
            StringBuilder builder = new StringBuilder();
            // base/type/id/_version/vid

            if (key.Base != null) 
            {
                builder.Append(key.Base);
                if (!key.Base.EndsWith("/")) builder.Append("/");
            }
            builder.Append(string.Format("{0}/{0}", key.TypeName, key.ResourceId));
            if (key.HasVersionId)
            {
                builder.Append(string.Format("/_history/{0}", key.VersionId));
            }
            return builder.ToString();
        }

        public static string RelativePath(this IKey self)
        {
            Key key = self.GetLocalKey();
            return key.ToString();
        }

        public static Uri ToRelativeUri(this IKey key)
        {
            return new Uri(key.ToString());
        }

        public static Uri ToUri(this IKey self)
        {
            return new Uri(self.Path());
        }

        public static Uri ToUri(this IKey key, Uri endpoint)
        {
            string _base = endpoint.ToString().TrimEnd('/');
            string s = string.Format("{0}/{1}", _base, key);
            return new Uri(s);
        }
          
        // Important! This is the core logic for the difference between an internal and external key.
        public static bool IsForeign(this IKey key)
        {
            if (key.Base == null) return false;
            
            return Localhost.IsEndpointOf(key.Base);
        }

        public static bool IsTemporary(this IKey key)
        {
            if (key.ResourceId != null)
            {
                return UriHelper.IsTemporaryUri(key.ResourceId);
            }
            else return false;
        }

        public static bool IsInternal(this IKey key)
        {
            return !(key.IsTemporary() || key.IsForeign());
        }

        

        

    }
}
