using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public enum KeyTriage { Foreign, Temporary, Local, Internal }

    public static class ILocalhostExtensions
    {
        public static bool IsLocal(this ILocalhost localhost, IKey key)
        {
            if (key.Base == null) return true;
            return localhost.IsEndpointOf(key.Base);
        }

        public static bool IsForeign(this ILocalhost localhost, IKey key)
        {
            return !localhost.IsLocal(key);
        }

        public static Key UriToKey(this ILocalhost localhost, Uri uri)
        {

            if (uri.IsAbsoluteUri)
            {
                if (localhost.IsBaseOf(uri))
                {
                    string path = localhost.GetOperationPath(uri);
                    return Key.ParseOperationPath(path);
                }
                else
                {
                    return null;
                    // or throw exception: unparsable uri;
                }
            }
            else
            {
                string path = uri.ToString();
                return Key.ParseOperationPath(path);
            }
        }

        public static KeyTriage Triage(this ILocalhost localhost, IKey key)
        {
            if (key.IsTemporary())
            {
                return KeyTriage.Temporary;
            }
            else if (!key.HasBase())
            {
                return KeyTriage.Internal;
            }
            else if (localhost.IsLocal(key))
            {
                return KeyTriage.Local;
            }
            else
            {
                return KeyTriage.Foreign;
            }
        }

        public static bool IsEndpointOf(this ILocalhost localhost, string uri)
        {
            return localhost.IsBaseOf(new Uri(uri));
        }

        public static string GetOperationPath(this ILocalhost localhost, Uri uri)
        {
            Uri endpoint = localhost.GetEndpointOf(uri);
            string _base = endpoint.ToString();
            string path = uri.ToString().Remove(0, _base.Length);
            return path;
        }
    }
}
