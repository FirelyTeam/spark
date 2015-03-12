/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Spark.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using Spark.Core;
using System.Net;
using Hl7.Fhir.Serialization;

namespace Spark.Http
{
    public static class HttpRequestFhirExtensions
    {
        
        public static void SaveBody(this HttpRequestMessage request, string contentType, byte[] data)
        {
            Binary b = new Binary { Content = data, ContentType = contentType };

            request.Properties.Add(Const.UNPARSED_BODY, b);
        }

        public static Binary GetBody(this HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey(Const.UNPARSED_BODY))
                return request.Properties[Const.UNPARSED_BODY] as Binary;
            else
                return null;
        }

        /// <summary>
        /// Temporary hack!
        /// Adds a resourceEntry to the request property bag. To be picked up by the MediaTypeFormatters for adding http headers.
        /// </summary>
        /// <param name="entry">The resource entry with information to generate headers</param>
        /// <remarks> 
        /// The SendAsync is called after the headers are set. The SetDefaultHeaders have no access to the content object.
        /// The only solution is to give the information through the Request Property Bag.
        /// </remarks>
        public static void SaveEntry(this HttpRequestMessage request, Interaction entry)
        {
            request.Properties.Add(Const.RESOURCE_ENTRY, entry);
        }

        public static Interaction GetEntry(this HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey(Const.RESOURCE_ENTRY))
                return request.Properties[Const.RESOURCE_ENTRY] as Interaction;
            else
                return null;
        }

        //public static HttpResponseMessage HttpResponse(this HttpRequestMessage request, HttpStatusCode code, Entry entry)
        //{
        //    request.SaveEntry(entry);

        //    HttpResponseMessage msg;
        //    msg = request.CreateResponse<Resource>(code, entry.Resource);
            
        //    // DSTU2: tags
        //    //msg.Headers.SetFhirTags(entry.Tags);
        //    return msg;
        //}

        public static HttpResponseMessage SetLocation(this HttpResponseMessage message, IKey key)
        {
            if (key != null)
                message.Content.Headers.Add("Content-Location", key.ToString());

            return message;
        }

        public static HttpResponseMessage HttpResponse(this HttpRequestMessage request, FhirResponse response)
        {
            HttpResponseMessage message;

            if (response.Resource != null)
            {
                message = request.CreateResponse(response.StatusCode, response.Resource);
                message.SetLocation(response.Key);
                // todo: it's preferable to add headers in the formatters level. But no generic solution found yet.
        
            }
            else
            {
                message = request.CreateResponse(response.StatusCode);
            }
            //message.Headers.Location = response.Key.ToUri(Localhost.Base);

            return message;
        }

        /*
        public static HttpResponseMessage HttpResponse(this HttpRequestMessage request, Entry entry)
        {
            return request.HttpResponse(HttpStatusCode.OK, entry);
        }
        */

        //public static HttpResponseMessage Error(this HttpRequestMessage request, int code, OperationOutcome outcome)
        //{
        //    return request.CreateResponse((HttpStatusCode)code, outcome);
        //}

        //public static HttpResponseMessage StatusResponse(this HttpRequestMessage request, Entry entry, HttpStatusCode code)
        //{
        //    request.SaveEntry(entry);
        //    HttpResponseMessage msg = request.CreateResponse(code);
        //    // DSTU2: tags
        //    // msg.Headers.SetFhirTags(entry.Tags); // todo: move to model binder
        //    msg.Headers.Location = entry.Key.ToUri(Localhost.Base);
        //    return msg;
        //}

        /*
        public static ICollection<Tag> GetFhirTags(this HttpRequestMessage request)
        {
            return request.Headers.GetFhirTags();
        }
        */

        public static DateTimeOffset? GetDateParameter(this HttpRequestMessage request, string name)
        {
            string param = request.GetParameter(name);
            if (param == null) return null;
            return DateTimeOffset.Parse(param);
        }

        public static int? GetIntParameter(this HttpRequestMessage request, string name)
        {
            string s = request.GetParameter(name);
            int n;
            return (int.TryParse(s, out n)) ? n : (int?)null;
        }

        public static bool? GetBooleanParameter(this HttpRequestMessage request, string name)
        {
            string s = request.GetParameter(name);           
            if(s == null) return null;

            try
            {
                bool b = PrimitiveTypeConverter.Convert<bool>(s);
                return (bool.TryParse(s, out b)) ? b : (bool?)null;
            }
            catch
            {
                return null;
            }
        }

        public static DateTimeOffset? IfModifiedSince(this HttpRequestMessage request)
        {
            string s = request.Header("If-modified-since");
            DateTimeOffset date;
            if (DateTimeOffset.TryParse(s, out date))
            {
                return date;
            }
            { 
                return null;
            }
        }

        public static string IfNoneMatch(this HttpRequestMessage request)
        {
            // The if-none-match can be either '*' or tags. This needs further implementation.
            string s = request.Header("If-none-match");
            return s;
        }

        public static string IfMatchVersionId(this HttpRequestMessage request)
        {
            return request.Header("If-match");
        }

        public static bool RequestSummary(this HttpRequestMessage request)
        {
            return (request.GetParameter("_summary") == "true");
        }

    }
}