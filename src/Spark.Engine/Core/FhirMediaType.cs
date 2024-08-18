/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2018-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Spark.Engine.Core
{
    public static class FhirMediaType
    {
        public static string DefaultJsonMimeType = ContentType.JSON_CONTENT_HEADER;
        public static string DefaultXmlMimeType = ContentType.XML_CONTENT_HEADER;
        public static string OctetStreamMimeType = "application/octet-stream";
        public static string FormUrlEncodedMimeType = "application/x-www-form-urlencoded";
        public static string AnyMimeType = "*/*";

        public static IEnumerable<string> JsonMimeTypes => ContentType.JSON_CONTENT_HEADERS;
        public static IEnumerable<string> XmlMimeTypes => ContentType.XML_CONTENT_HEADERS;
        public static IEnumerable<string> SupportedMimeTypes => JsonMimeTypes
            .Concat(XmlMimeTypes)
            .Concat(new[] { OctetStreamMimeType, FormUrlEncodedMimeType, AnyMimeType });

        /// <summary>
        /// Transforms loose formats to their strict variant
        /// </summary>
        /// <param name="format">Mime type</param>
        /// <returns></returns>
        public static string Interpret(string format)
        {
            if (format == null) return DefaultJsonMimeType;
            if (XmlMimeTypes.Contains(format)) return DefaultXmlMimeType;
            if (JsonMimeTypes.Contains(format)) return DefaultJsonMimeType;
            return format;
        }

        public static ResourceFormat GetResourceFormat(string format)
        {
            string strict = Interpret(format);
            if (strict == DefaultXmlMimeType) return ResourceFormat.Xml;
            else if (strict == DefaultJsonMimeType) return ResourceFormat.Json;
            else return ResourceFormat.Xml;
        }

        public static string GetContentType(Type type, ResourceFormat format) 
        {
            if (typeof(Resource).IsAssignableFrom(type) || type == typeof(Resource))
            {
                switch (format)
                {
                    case ResourceFormat.Json: return DefaultJsonMimeType;
                    case ResourceFormat.Xml: return DefaultXmlMimeType;
                    default: return DefaultXmlMimeType;
                }
            }
            else 
                return OctetStreamMimeType;
        }

        public static string GetMediaType(this HttpRequestMessage request)
        {
            MediaTypeHeaderValue headervalue = request.Content.Headers.ContentType;
            string s = headervalue?.MediaType;
            return Interpret(s);
        }

        public static string GetContentTypeHeaderValue(this HttpRequestMessage request)
        {
            MediaTypeHeaderValue headervalue = request.Content.Headers.ContentType;
            return headervalue?.MediaType;
        }

        public static string GetAcceptHeaderValue(this HttpRequestMessage request)
        {
            HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue> headers = request.Headers.Accept;
            return headers.FirstOrDefault()?.MediaType;
        }

        public static MediaTypeHeaderValue GetMediaTypeHeaderValue(Type type, ResourceFormat format)
        {
            string mediatype = GetContentType(type, format);
            MediaTypeHeaderValue header = new MediaTypeHeaderValue(mediatype)
            {
                CharSet = Encoding.UTF8.WebName
            };
            return header;
        }
    }
}