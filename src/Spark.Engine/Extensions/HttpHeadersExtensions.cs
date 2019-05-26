/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Hl7.Fhir.Model;
#if NETSTANDARD2_0
using Microsoft.Extensions.Primitives;
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
        
        public static void Replace(this HttpHeaders headers, string header, string value)
        {
            //if (headers.Exists(header)) 
            headers.Remove(header);
            headers.Add(header, value);
        }
        
        public static string Value(this HttpHeaders headers, string key)
        {
            IEnumerable<string> values;
            if (headers.TryGetValues(key, out values))
            {
                return values.FirstOrDefault();
            }
            else return null;
        }

        /// <summary>
        /// Returns true if the Content-Type header matches any of the supported Xml or Json MIME types.
        /// </summary>
        /// <param name="content">An instance of <see cref="HttpContent"/>.</param>
        /// <returns>Returns true if the Content-Type header matches any of the supported Xml or Json MIME types.</returns>
        public static bool IsContentTypeHeaderFhirMediaType(this HttpContent content)
        {
            string contentType = content.Headers.ContentType?.MediaType;
            return ContentType.XML_CONTENT_HEADERS.Contains(contentType)
                || ContentType.JSON_CONTENT_HEADERS.Contains(contentType);
        }

        public static void ReplaceHeader(this HttpRequestMessage request, string header, string value)
        {
            request.Headers.Replace(header, value);
        }

        public static string Header(this HttpRequestMessage request, string key)
        {
            IEnumerable<string> values;
            if (request.Content.Headers.TryGetValues(key, out values))
            {
                return values.FirstOrDefault();
            }
            else return null;
        }

        public static string GetParameter(this HttpRequestMessage request, string key)
        {
            NameValueCollection queryNameValuePairs = request.RequestUri.ParseQueryString();
            foreach (var currentKey in queryNameValuePairs.AllKeys)
            {
                if (currentKey == key) return queryNameValuePairs[currentKey];
            }
            return null;
        }

        public static List<Tuple<string, string>> TupledParameters(this HttpRequestMessage request)
        {
            var list = new List<Tuple<string, string>>();

            NameValueCollection queryNameValuePairs = request.RequestUri.ParseQueryString();
            foreach (var currentKey in queryNameValuePairs.AllKeys)
            {
                list.Add(new Tuple<string, string>(currentKey, queryNameValuePairs[currentKey]));
            }
            return list;
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
    public static class HttpHeadersFhirExtensions
    {
        public static bool IsSummary(this HttpHeaders headers)
        {
            string summary = headers.Value("_summary");
            return (summary != null) ? summary.ToLower() == "true" : false;
        }
    }
}