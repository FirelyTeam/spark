using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public static class UriHelper
    {
        public const string CID = "cid";

        public static string CreateCID()
        {
            return string.Format("{0}:{1}", CID, Guid.NewGuid());
        }

        public static Uri CreateUrn()
        {
            return new Uri("urn:guid:" + Guid.NewGuid());
        }

        public static Uri CreateUuid()
        {
            return new Uri("urn:uuid:" + Guid.NewGuid().ToString());
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

        public static bool IsTemporaryUri(this Uri uri)
        {
            if (uri == null) return false;

            return IsTemporaryUri(uri.ToString());
        }

        public static bool IsTemporaryUri(string uri)
        {
            return uri.StartsWith("urn:uuid:")
                || uri.StartsWith("urn:guid:")
                || uri.StartsWith("cid:");
        }

        public static Uri HistoryKeyFor(this IGenerator generator, Uri key)
        {
            var identity = new ResourceIdentity(key);
            string vid = generator.NextVersionId(identity.ResourceType);
            Uri result = identity.WithVersion(vid);
            return result;
        }

    }
}
