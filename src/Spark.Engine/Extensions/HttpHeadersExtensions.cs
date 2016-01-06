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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

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
            foreach (var param in request.GetQueryNameValuePairs())
            {
                if (param.Key == key) return param.Value;
            }
            return null;
        }

        public static List<Tuple<string, string>> TupledParameters(this HttpRequestMessage request)
        {
            var list = new List<Tuple<string, string>>();

            IEnumerable<KeyValuePair<string, string>> query = request.GetQueryNameValuePairs();
            foreach (var pair in query)
            {
                list.Add(new Tuple<string, string>(pair.Key, pair.Value));
            }
            return list;
        }

        public static SearchParams GetSearchParams(this HttpRequestMessage request)
        {
            var parameters = request.TupledParameters().Where(tp => tp.Item1 != "_format");
            UriParamList actualParameters = new UriParamList(parameters);
            var searchCommand = SearchParams.FromUriParamList(parameters);
            return searchCommand;
        }
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