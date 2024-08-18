/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly)
 * Copyright (c) 2018-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using Tasks = System.Threading.Tasks;
using System.Xml;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text;
using Hl7.Fhir.Rest;
using Spark.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Core;
using Spark.Engine.Auxiliary;

namespace Spark.Formatters
{
    public class XmlFhirFormatter : FhirMediaTypeFormatter
    {
        private readonly FhirXmlParser _parser;
        private readonly FhirXmlSerializer _serializer;

        public XmlFhirFormatter(FhirXmlParser parser, FhirXmlSerializer serializer) : base()
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            foreach (var mediaType in ContentType.XML_CONTENT_HEADERS)
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
            }
        }

        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            base.SetDefaultContentHeaders(type, headers, mediaType);
            headers.ContentType = FhirMediaType.GetMediaTypeHeaderValue(type, ResourceFormat.Xml);
        }

        public override Tasks.Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            try
            {
                var body = base.ReadBodyFromStream(readStream, content);

                if (type == typeof(Bundle))
                {
                    if (XmlSignatureHelper.IsSigned(body))
                    {
                        if (!XmlSignatureHelper.VerifySignature(body))
                            throw Error.BadRequest("Digital signature in body failed verification");
                    }
                }

                if (typeof(Resource).IsAssignableFrom(type))
                {
                    Resource resource = _parser.Parse<Resource>(body);
                    return Tasks.Task.FromResult<object>(resource);
                }
                else
                    throw Error.Internal("The type {0} expected by the controller can not be deserialized", type.Name);
            }
            catch (FormatException exc)
            {
                throw Error.BadRequest("Body parsing failed: " + exc.Message);
            }
        }

        public override Tasks.Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            XmlWriter writer = new XmlTextWriter(writeStream, new UTF8Encoding(false));
            SummaryType summary = requestMessage.RequestSummary();

            if (type == typeof(OperationOutcome)) 
            {
                Resource resource = (Resource)value;
                _serializer.Serialize(resource, writer, summary);
            }
            else if (typeof(Resource).IsAssignableFrom(type))
            {
                Resource resource = (Resource)value;
                _serializer.Serialize(resource, writer, summary);
            }
            else if (type == typeof(FhirResponse))
            {
                FhirResponse response = (value as FhirResponse);
                if (response.HasBody)
                    _serializer.Serialize(response.Resource, writer, summary);
            }

            writer.Flush();
            return Tasks.Task.CompletedTask;
        }
    }
}
