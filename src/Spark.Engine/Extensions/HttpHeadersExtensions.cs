﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
#if NETSTANDARD2_0
using Microsoft.AspNetCore.Http;
#endif

namespace Spark.Engine.Extensions
{
    public static class HttpRequestExtensions
    {
        
        public static bool Exists(this HttpHeaders headers, string key)
        {
            IEnumerable<string> values;
            if (headers.TryGetValues(key, out values))
            {
                return values.Count() > 0;
            }
            else return false;

        }
        
        internal static void Replace(this HttpHeaders headers, string header, string value)
        {
            //if (headers.Exists(header)) 
            headers.Remove(header);
            headers.Add(header, value);
        }

        /// <summary>
        /// Returns true if the Content-Type header matches any of the supported Xml or Json MIME types.
        /// </summary>
        /// <param name="content">An instance of <see cref="HttpContent"/>.</param>
        /// <returns>Returns true if the Content-Type header matches any of the supported Xml or Json MIME types.</returns>
        internal static bool IsContentTypeHeaderFhirMediaType(this HttpContent content)
        {
            return IsContentTypeHeaderFhirMediaType(content.Headers.ContentType?.MediaType);
        }
        public static bool IsContentTypeHeaderFhirMediaType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return false;
            return ContentType.XML_CONTENT_HEADERS.Contains(contentType)
                || ContentType.JSON_CONTENT_HEADERS.Contains(contentType);
        }

#if NETSTANDARD2_0
        public static string GetParameter(this HttpRequest request, string key)
        {
            string value = null;
            if(request.Query.ContainsKey(key))
                value = request.Query.FirstOrDefault(p => p.Key == key).Value.FirstOrDefault();
            return value;
        }

        public static List<Tuple<string, string>> TupledParameters(this HttpRequest request)
        {
            var list = new List<Tuple<string, string>>();

            IQueryCollection queryCollection = request.Query;
            foreach(var query in queryCollection)
            {
                list.Add(new Tuple<string, string>(query.Key, query.Value));
            }
            return list;
        }

        public static SearchParams GetSearchParams(this HttpRequest request)
        {
            var parameters = request.TupledParameters().Where(tp => tp.Item1 != "_format");
            var searchCommand = SearchParams.FromUriParamList(parameters);
            return searchCommand;
        }
#endif

        public static SearchParams GetSearchParams(this HttpRequestMessage request)
        {
            var parameters = request.TupledParameters().Where(tp => tp.Item1 != "_format");
            var searchCommand = SearchParams.FromUriParamList(parameters);
            return searchCommand;
        }
    }

    public static class FhirHttpHeaders
    {
        public const string IfNoneExist = "If-None-Exist";
    }
}