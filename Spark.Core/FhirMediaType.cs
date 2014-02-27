using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
//using System.Web.Http;

namespace Spark.Core
{ 
    // ballot: JSON MimeType overlap is inconsistent 
    /*
    More logical would be: 
            application/fhir+xml
            application/fhir+bundle+xml  
            application/fhir+atom+xml
    
            application/fhir+json, 
            application/fhir+bundle+json
    */
    public static class FhirMediaType
    {
        // todo: This class can be merged into HL7.Fhir.ContentType

        public const string XmlResource = "application/xml+fhir";
        public const string XmlBundle = "application/atom+xml";
        public const string XmlTagList = "application/xml+fhir";

        public const string JsonResource = "application/json+fhir";
        public const string JsonBundle = "application/json+fhir";
        public const string JsonTagList = "application/json+fhir";

        public const string BinaryResource = "application/fhir+binary";

        public static ICollection<string> StrictFormats 
        {
            get 
            {
                return new List<string>() { XmlResource, XmlBundle, JsonResource, JsonBundle };
            }
        }
        public static string[] LooseXmlFormats = { "xml", "text/xml", "application/xml" };
        public static readonly string[] LooseJsonFormats = { "json", "application/json" };

        public static string Interpret(string format)
        {
            if (format == null) return XmlResource;
            if (StrictFormats.Contains(format)) return format;
            else if (LooseXmlFormats.Contains(format)) return XmlResource;
            else if (LooseJsonFormats.Contains(format)) return JsonResource;
            //else throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            else return format;
        }
        public static ResourceFormat GetResourceFormat(string format)
        {
            string strict = Interpret(format);
            if ((strict == XmlResource) || (strict == XmlBundle)) return ResourceFormat.Xml;
            else if ((strict == JsonResource) || (strict == JsonBundle)) return ResourceFormat.Json;
            else return ResourceFormat.Xml;

        }

        public static string GetContentType(Type type, ResourceFormat format) 
        {
            if (typeof(Resource).IsAssignableFrom(type) || type == typeof(ResourceEntry))
            {
                switch (format)
                {
                    case ResourceFormat.Json: return JsonResource;
                    case ResourceFormat.Xml: return XmlResource;
                    default: return XmlResource;
                }
            }
            else if (type == typeof(Bundle))
            {
                switch (format)
                {
                    case ResourceFormat.Json: return JsonBundle;
                    case ResourceFormat.Xml: return XmlBundle;
                    default: return XmlBundle;
                }
            }
            else if (type == typeof(TagList))
            {
                switch (format)
                {
                    case ResourceFormat.Json: return JsonTagList;
                    case ResourceFormat.Xml: return XmlTagList;
                    default: return XmlTagList;
                }
            }
            else 
                return "application/octet-stream";
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