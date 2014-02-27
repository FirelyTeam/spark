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
using Newtonsoft.Json;
using Spark.Support;
using Hl7.Fhir.Rest;
using System.Text;
using Spark.Core;

namespace Spark.Filters
{
    public class JsonFhirFormatter : FhirMediaTypeFormatter
    {
        public JsonFhirFormatter() : base()
        {
            foreach (var mediaType in ContentType.JSON_CONTENT_HEADERS)
                SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
        }
        
        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            base.SetDefaultContentHeaders(type, headers, mediaType);
            headers.ContentType = FhirMediaType.GetMediaTypeHeaderValue(type, ResourceFormat.Json);
         //   headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "fhir.resource.json" };
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            return Task.Factory.StartNew<object>(() => 
            {
                try
                {
                    var body = base.ReadBodyFromStream(readStream, content);

                    if (type == typeof(ResourceEntry))
                    {
                        Resource resource = FhirParser.ParseResourceFromJson(body);
                        ResourceEntry entry = ResourceEntry.Create(resource);
                        entry.Tags = content.Headers.GetFhirTags();
                        return entry;
                    }
                    else if (type == typeof(Bundle))
                    {
                        return FhirParser.ParseBundleFromJson(body);
                    }
                    else if (type == typeof(TagList))
                    {
                        return FhirParser.ParseTagListFromJson(body);
                    }
                    else
                        throw new NotSupportedException(String.Format("Cannot read unsupported type {0} from body", type.Name));
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
                StreamWriter writer = new StreamWriter(writeStream);
                JsonWriter jsonwriter = new JsonTextWriter(writer);
                if (type == typeof(OperationOutcome))
                {
                    Resource resource = (Resource)value;
                    FhirSerializer.SerializeResource(resource, jsonwriter);
                }
                else if (type == typeof(ResourceEntry))
                {
                    ResourceEntry entry = (ResourceEntry)value;
                    FhirSerializer.SerializeResource(entry.Resource, jsonwriter);
                    content.Headers.SetFhirTags(entry.Tags);
                }
                else if (type == typeof(Bundle))
                {
                    FhirSerializer.SerializeBundle((Bundle)value, jsonwriter);
                }
                else if (type == typeof(TagList))
                {
                    FhirSerializer.SerializeTagList((TagList)value, jsonwriter);
                }

                writer.Flush();
            });
        }
    }
}
