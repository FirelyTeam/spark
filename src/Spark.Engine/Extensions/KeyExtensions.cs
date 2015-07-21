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
using System.Text;
using Spark.Engine.Core;

namespace Spark.Engine.Extensions
{

    public static class KeyExtensions
    {
        public static Key ExtractKey(this Resource resource)
        {
            string _base = (resource.ResourceBase != null) ? resource.ResourceBase.ToString() : null;
            Key key = new Key(_base, resource.TypeName, resource.Id, resource.VersionId);
            return key;
        }

        public static Key ExtractKey(Uri uri)
        {
            
            var identity = new ResourceIdentity(uri);
            
            string _base = (identity.HasBaseUri) ? identity.BaseUri.ToString() : null;
            Key key = new Key(_base, identity.ResourceType, identity.Id, identity.VersionId);
            return key;
        }

        public static Key ExtractKey(this Localhost localhost, Bundle.BundleEntryComponent entry)
        {
            Uri uri = new Uri(entry.Transaction.Url, UriKind.RelativeOrAbsolute);
            Key compare = ExtractKey(uri); // This fails!! ResourceIdentity does not work in this case.
            return localhost.LocalUriToKey(uri);   
            
        }
                
        public static void ApplyTo(this IKey key, Resource resource)
        {
            resource.ResourceBase = key.HasBase() ?  new Uri(key.Base) : null;
            resource.Id = key.ResourceId;
            resource.VersionId = key.VersionId;
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

        public static bool HasBase(this IKey key)
        {
            return !string.IsNullOrEmpty(key.Base);
        }

        public static Key WithBase(this IKey self, string _base)
        {
            Key key = self.Clone();
            key.Base = _base;
            return key;
        }

        public static Key WithoutBase(this IKey self)
        {
            Key key = self.Clone();
            key.Base = null;
            return key;
        }


        public static Key WithoutVersion(this IKey self)
        {
            Key key = self.Clone();
            key.VersionId = null;
            return key;
        }

        public static bool HasVersionId(this IKey self)
        {
            return !string.IsNullOrEmpty(self.VersionId);
        }

        public static bool HasResourceId(this IKey self)
        {
            return !string.IsNullOrEmpty(self.ResourceId);
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
            builder.Append(string.Format("{0}/{1}", key.TypeName, key.ResourceId));
            if (key.HasVersionId())
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

        // A local reference is of the format <resource>/<id>/history/<id>
        // This ensures a server wide unique id. 
        public static string ToLocalReference(this IKey key)
        {
            return key.GetLocalKey().Path();
        }

        public static Key CreateFromLocalReference(string reference)
        {
            string[] parts = reference.Split('/');
            if (parts.Length == 2)
            {
                return Key.Create(parts[0], parts[1], parts[3]);
            }
            else if (parts.Length == 4)
            {
                return Key.Create(parts[0], parts[1], parts[3]);
            }
            else throw new ArgumentException("Could not create key from local-reference: " + reference);
        }

        public static Uri ToRelativeUri(this IKey key)
        {
            string path = key.RelativePath();
            return new Uri(path, UriKind.Relative);
        }

        

        public static Uri ToUri(this IKey self)
        {
            return new Uri(self.Path(), (self.Base == null) ? UriKind.Relative : UriKind.Absolute);
        }

        public static Uri ToUri(this IKey key, Uri endpoint)
        {
            string _base = endpoint.ToString().TrimEnd('/');
            string s = string.Format("{0}/{1}", _base, key);
            return new Uri(s);
        }
          
        // Important! This is the core logic for the difference between an internal and external key.
        
        public static bool IsTemporary(this IKey key)
        {
            if (key.ResourceId != null)
            {
                return UriHelper.IsTemporaryUri(key.ResourceId);
            }
            else return false;
        }

        public static bool SameAs(this IKey key, IKey other)
        {
            return (key.Base == other.Base)
                && (key.TypeName == other.TypeName)
                && (key.ResourceId == other.ResourceId)
                && (key.VersionId == other.VersionId);
        
        }

        

        

    }
}
