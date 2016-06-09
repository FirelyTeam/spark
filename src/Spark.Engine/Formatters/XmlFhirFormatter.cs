/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
        public XmlFhirFormatter() : base()
        {
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

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            return Task.Factory.StartNew<object>( () => 
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
                        Resource resource = FhirParser.ParseResourceFromXml(body);
                        return resource;
                    }
                    else
                        throw Error.Internal("The type {0} expected by the controller can not be deserialized", type.Name);
                }
                catch (FormatException exc)
                {
                    throw Error.BadRequest("Body parsing failed: " + exc.Message);
                }
            });
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            
            return Task.Factory.StartNew(() =>
            {
                XmlWriter writer = new XmlTextWriter(writeStream, new UTF8Encoding(false));
                SummaryType summary = requestMessage.RequestSummary();

                if (type == typeof(OperationOutcome)) 
                {
                    Resource resource = (Resource)value;
                    FhirSerializer.SerializeResource(resource, writer, summary);
                }
                else if (typeof(Resource).IsAssignableFrom(type))
                {
                    Resource resource = (Resource)value;
                    FhirSerializer.SerializeResource(resource, writer, summary);
                }
                else if (type == typeof(FhirResponse))
                {
                    FhirResponse response = (value as FhirResponse);
                    if (response.HasBody)
                    FhirSerializer.SerializeResource(response.Resource, writer, summary);
                }
                
                writer.Flush();
            });
        }
    }
}
