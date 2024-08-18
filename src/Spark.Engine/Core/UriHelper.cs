/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly)
 * Copyright (c) 2017-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Rest;
using System;
using Spark.Core;

namespace Spark.Engine.Core
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


        /// <summary>
        /// Determines wether the uri contains a hash (#) frament.
        /// </summary>
        public static bool HasFragment(this Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                string fragment = uri.Fragment;
                return !string.IsNullOrEmpty(fragment);
            }
            else
            {
                string s = uri.ToString();
                return s.StartsWith("#");
            }
        }

        public static Uri HistoryKeyFor(this IIdentityGenerator generator, Uri key)
        {
            var identity = new ResourceIdentity(key);
            string vid = generator.NextVersionId(identity.ResourceType, identity.Id);
            Uri result = identity.WithVersion(vid);
            return result;
        }

        /// <summary>
        /// Bugfixed_IsBaseOf is a fix for Uri.IsBaseOf which has a bug
        /// </summary>
        public static bool Bugfixed_IsBaseOf(this Uri _base, Uri uri)
        {
            string b = _base.ToString().ToLowerInvariant();
            string u = uri.ToString().ToLowerInvariant();

            bool isbase = u.StartsWith(b);
            return isbase;
        }
    }
}
