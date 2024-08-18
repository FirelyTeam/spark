/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2019-2024, Incendi (info@incendi.no) and contributors
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
#if NETSTANDARD2_0 || NET6_0
using Microsoft.AspNetCore.Http;
#endif

namespace Spark.Engine.Extensions
{
    public static class HttpRequestExtensions
    {
        public static bool Exists(this HttpHeaders headers, string key)
        {
            if (headers.TryGetValues(key, out IEnumerable<string> values))
            {
                return values.Count() > 0;
            }
            else
            {
                return false;
            }
        }
        
        internal static void Replace(this HttpHeaders headers, string header, string value)
        {
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

#if NETSTANDARD2_0 || NET6_0
        public static string GetParameter(this HttpRequest request, string key)
        {
            string value = null;
            if(request.Query.ContainsKey(key))
                value = request.Query.FirstOrDefault(p => p.Key == key).Value.FirstOrDefault();
            return value;
        }

        public static List<Tuple<string, string>> TupledParameters(this HttpRequest request)
        {
            return UriParamList.FromQueryString(request.QueryString.Value);
        }

        public static SearchParams GetSearchParamsFromBody(this HttpRequest request)
        {
            var list = new List<Tuple<string, string>>();
            foreach (var parameter in request.Form)
            {
                list.AddRange(parameter.Value.Select(value => new Tuple<string, string>(parameter.Key, value)));
            }
            return request.GetSearchParams().AddAll(list);
        }

        public static SearchParams GetSearchParams(this HttpRequest request)
        {
            var parameters = request.TupledParameters().Where(tp => tp.Item1 != "_format");
            var searchCommand = SearchParams.FromUriParamList(parameters);
            return searchCommand;
        }
#endif

        public static SearchParams GetSearchParamsFromBody(this HttpRequestMessage request)
        {
            var list = new List<Tuple<string, string>>();
            string content = request.Content.ReadAsStringAsync().Result;
            string[] parameters = string.IsNullOrEmpty(content) ? new string[0] : content.Split('&');
            foreach (string parameter in parameters)
            {
                string[] p = parameter.Split('=');
                list.Add(new Tuple<string, string>(p[0], Uri.UnescapeDataString(p[1])));
            }

            return request.GetSearchParams().AddAll(list);
        }

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