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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
//using System.Web.Http;

namespace Spark.Engine.Core
{

    public static class FhirMediaType
    {
        // API: This class can be merged into HL7.Fhir.Rest.ContentType

        public const string XmlResource = "application/xml+fhir";
        public const string XmlTagList = "application/xml+fhir";

        public const string JsonResource = "application/json+fhir";
        public const string JsonTagList = "application/json+fhir";

        public const string BinaryResource = "application/fhir+binary";

        public static ICollection<string> StrictFormats 
        {
            get 
            {
                return new List<string>() { XmlResource, JsonResource };
            }
        }
        public static string[] LooseXmlFormats = { "xml", "text/xml", "application/xml" };
        public static readonly string[] LooseJsonFormats = { "json", "application/json" };

        /// <summary>
        /// Transforms loose formats to their strict variant
        /// </summary>
        /// <param name="format">Mime type</param>
        /// <returns></returns>
        public static string Interpret(string format)
        {
            if (format == null) return XmlResource;
            if (StrictFormats.Contains(format)) return format;
            if (LooseXmlFormats.Contains(format)) return XmlResource;
            if (LooseJsonFormats.Contains(format)) return JsonResource;
            return format;
        }

        public static ResourceFormat GetResourceFormat(string format)
        {
            string strict = Interpret(format);
            if (strict == XmlResource) return ResourceFormat.Xml;
            else if (strict == JsonResource) return ResourceFormat.Json;
            else return ResourceFormat.Xml;

        }

        public static string GetContentType(Type type, ResourceFormat format) 
        {
            if (typeof(Resource).IsAssignableFrom(type) || type == typeof(Resource))
            {
                switch (format)
                {
                    case ResourceFormat.Json: return JsonResource;
                    case ResourceFormat.Xml: return XmlResource;
                    default: return XmlResource;
                }
            }
            else 
                return "application/octet-stream";
        }

        public static string GetMediaType(this HttpRequestMessage request)
        {
            MediaTypeHeaderValue headervalue = request.Content.Headers.ContentType;
            string s = (headervalue != null) ? headervalue.MediaType : null;
            return Interpret(s);
        }

        public static MediaTypeHeaderValue GetMediaTypeHeaderValue(Type type, ResourceFormat format)
        {
            string mediatype = FhirMediaType.GetContentType(type, format);
            MediaTypeHeaderValue header = new MediaTypeHeaderValue(mediatype);
            header.CharSet = Encoding.UTF8.WebName;
            return header;
        }
    }

   

}