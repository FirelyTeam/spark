/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text;
using Hl7.Fhir.Rest;
using Spark.Core;
using Spark.Service;

namespace Spark.Formatters
{
    public class XmlFhirFormatter : FhirMediaTypeFormatter
    {
        public XmlFhirFormatter() : base()
        {
            foreach (var mediaType in ContentType.XML_CONTENT_HEADERS)
                SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
        }

        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            base.SetDefaultContentHeaders(type, headers, mediaType);
            headers.ContentType = FhirMediaType.GetMediaTypeHeaderValue(type, ResourceFormat.Xml);
          //  headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "fhir.resource.xml" };
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
                                throw new SparkException(HttpStatusCode.BadRequest, "Digital signature in body failed verification");
                        }
                    }

                    if (type == typeof(Resource))
                    {
                        Resource resource = FhirParser.ParseResourceFromXml(body);
                        //ResourceEntry entry = ResourceEntry.Create(resource);
                        //entry.Tags = content.Headers.GetFhirTags();
                        return resource;
                    }
                    else
                        throw new NotSupportedException(String.Format("Cannot read unsupported type {0} from body",type.Name));
                }
                catch (FormatException exc)
                {
                    throw new SparkException(HttpStatusCode.BadRequest, "Body parsing failed: " + exc.Message);
                }
            });
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            return Task.Factory.StartNew(() =>
            {
                XmlWriter writer = new XmlTextWriter(writeStream, Encoding.UTF8);

                bool summary = content.Headers.IsSummary();

                if (type == typeof(OperationOutcome)) 
                {
                    Resource resource = (Resource)value;
                    FhirSerializer.SerializeResource(resource, writer, summary);
                }
                else if (type == typeof(Resource))
                {
                    Resource resource = (Resource)value;
                    FhirSerializer.SerializeResource(resource, writer, summary);

                    content.Headers.ContentLocation = resource.ExtractKey().ToUri(Localhost.Base);
                    
                    //content.Headers.SetFhirTags(entry.Tags);
                }
                else if (type == typeof(Response))
                {
                    Response response = (value as Response);
                    if (response.HasBody)
                    FhirSerializer.SerializeResource(response.Resource, writer, summary);
                    content.Headers.ContentLocation = response.Key.ToUri(Localhost.Base);
                }
                
                writer.Flush();
            });
        }
    }
}
