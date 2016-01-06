using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using Spark.Engine.Extensions;

namespace Spark.Engine.Core
{
    public static class ILocalhostExtensions
    {
        public static bool IsLocal(this ILocalhost localhost, IKey key)
        {
            if (key.Base == null) return true;
            return localhost.IsBaseOf(key.Base);
        }

        public static bool IsForeign(this ILocalhost localhost, IKey key)
        {
            return !localhost.IsLocal(key);
        }

        public static Uri RemoveBase(this ILocalhost localhost, Uri uri)
        {
            string s = uri.ToString();
            string _base = localhost.GetBaseOf(uri)?.ToString();
            string path = s.Remove(0, _base == null ? 0 : _base.Length);
            return new Uri(path, UriKind.Relative);
        }

        public static Key LocalUriToKey(this ILocalhost localhost, Uri uri)
        {
            string s = uri.ToString();
            string _base = localhost.GetBaseOf(uri)?.ToString();
            string path = s.Remove(0, _base == null ? 0 : _base.Length);

            return Key.ParseOperationPath(path).WithBase(_base);
        }

        public static Key UriToKey(this ILocalhost localhost, Uri uri)
        {

            if (uri.IsAbsoluteUri)
            {
                if (localhost.IsBaseOf(uri))
                {
                    return localhost.LocalUriToKey(uri);
                }
                else
                {
                    throw new ArgumentException("Cannot create a key from a foreign Uri");
                }
            }
            else
            {
                string path = uri.ToString();
                return Key.ParseOperationPath(path);
            }
        }
        
        public static Key UriToKey(this ILocalhost localhost, string uristring)
        {
            Uri uri = new Uri(uristring, UriKind.RelativeOrAbsolute);
            return localhost.UriToKey(uri);
        }

        public static Uri GetAbsoluteUri(this ILocalhost localhost, IKey key)
        {
            return key.ToUri(localhost.DefaultBase);
        }

        public static KeyKind GetKeyKind(this ILocalhost localhost, IKey key)
        {
            if (key.IsTemporary())
            {
                return KeyKind.Temporary;
            }
            else if (!key.HasBase())
            {
                return KeyKind.Internal;
            }
            else if (localhost.IsLocal(key))
            {
                return KeyKind.Local;
            }
            else
            {
                return KeyKind.Foreign;
            }
        }

        public static bool IsBaseOf(this ILocalhost localhost, string uristring)
        {
            Uri uri = new Uri(uristring, UriKind.RelativeOrAbsolute);
            return localhost.IsBaseOf(uri);
        }

        //public static string GetOperationPath(this ILocalhost localhost, Uri uri)
        //{
        //    Key key = localhost.AnyUriToKey(uri).WithoutBase();

        //    return key.ToOperationPath();
        //    //Uri endpoint = localhost.GetBaseOf(uri);
        //    //string _base = endpoint.ToString();
        //    //string path = uri.ToString().Remove(0, _base.Length);
        //    //return path;
        //}

        public static Uri Uri(this ILocalhost localhost, params string[] segments)
        {
            return new RestUrl(localhost.DefaultBase).AddPath(segments).Uri;
        }

        public static Uri Uri(this ILocalhost localhost, IKey key)
        {
            return key.ToUri(localhost.DefaultBase);
        }

        public static Bundle CreateBundle(this ILocalhost localhost, Bundle.BundleType type)
        {
            Bundle bundle = new Bundle();
            bundle.Type = type;
            return bundle;
        }
    }

}
