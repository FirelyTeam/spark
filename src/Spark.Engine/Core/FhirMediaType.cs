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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Spark.Engine.Core
{
    public static class FhirMediaType
    {
        public const string OCTET_STREAM_CONTENT_HEADER = "application/octet-stream";

        /// <summary>
        /// Transforms loose formats to their strict variant
        /// </summary>
        /// <param name="format">Mime type</param>
        /// <returns></returns>
        public static string Interpret(string format)
        {
            if (format == null) return ContentType.JSON_CONTENT_HEADER;
            if (ContentType.XML_CONTENT_HEADERS.Contains(format)) return ContentType.XML_CONTENT_HEADER;
            if (ContentType.JSON_CONTENT_HEADERS.Contains(format)) return ContentType.JSON_CONTENT_HEADER;
            return format;
        }

        public static ResourceFormat GetResourceFormat(string format)
        {
            string strict = Interpret(format);
            if (strict == ContentType.XML_CONTENT_HEADER) return ResourceFormat.Xml;
            else if (strict == ContentType.JSON_CONTENT_HEADER) return ResourceFormat.Json;
            else return ResourceFormat.Xml;
        }

        public static string GetContentType(Type type, ResourceFormat format) 
        {
            if (typeof(Resource).IsAssignableFrom(type) || type == typeof(Resource))
            {
                switch (format)
                {
                    case ResourceFormat.Json: return ContentType.JSON_CONTENT_HEADER;
                    case ResourceFormat.Xml: return ContentType.XML_CONTENT_HEADER;
                    default: return ContentType.XML_CONTENT_HEADER;
                }
            }
            else 
                return "application/octet-stream";
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