/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Spark.Http;
using System.IO;
using Spark.Core;
using System.Net;

namespace Spark.Formatters
{
    public abstract class FhirMediaTypeFormatter : MediaTypeFormatter
    {
        public FhirMediaTypeFormatter() : base()
        {
            this.SupportedEncodings.Clear();
            this.SupportedEncodings.Add(Encoding.UTF8);
        }

        protected Interaction entry = null;

        private void setEntryHeaders(HttpContentHeaders headers)
        {
            if (entry != null)
            {
                headers.LastModified = entry.When;
                headers.ContentLocation = entry.Key.ToUri(Localhost.Base);

                if (entry.Resource is Binary)
                {
                    Binary binary = (Binary)entry.Resource;
                    headers.ContentType = new MediaTypeHeaderValue(binary.ContentType);
                }
            }
        }

        public override bool CanReadType(Type type)
        {
            return type == typeof(Resource) /* || type == typeof(Bundle) || (type == typeof(TagList) ) */ ;
        }

        public override bool CanWriteType(Type type)
        {
            return type == typeof(Resource) || type == typeof(OperationOutcome) /* || type == typeof(Bundle) || (type == typeof(TagList)) || type == typeof(OperationOutcome ) */ ;
        }

        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            base.SetDefaultContentHeaders(type, headers, mediaType);
            setEntryHeaders(headers);
        }

        public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
        {
            this.entry = request.GetEntry();
            return base.GetPerRequestFormatterInstance(type, request, mediaType);
        }

        protected string ReadBodyFromStream(Stream readStream, HttpContent content)
        {
            var charset = content.Headers.ContentType.CharSet ?? Encoding.UTF8.HeaderName;
            var encoding = Encoding.GetEncoding(charset);

            if (encoding != Encoding.UTF8)
                throw new SparkException(HttpStatusCode.BadRequest, "FHIR supports UTF-8 encoding exclusively, not " + encoding.WebName);

            StreamReader sr = new StreamReader(readStream, Encoding.UTF8, true);
            return sr.ReadToEnd();
        }

    }

}