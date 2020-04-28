#if NETSTANDARD2_0
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc.Formatters;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Spark.Engine.Formatters
{
    public class BinaryOutputFormatter : OutputFormatter
    {
        public BinaryOutputFormatter()
        {
            SupportedMediaTypes.Add(FhirMediaType.OCTET_STREAM_CONTENT_HEADER);
        }

        protected override bool CanWriteType(Type type)
        {
            return typeof(Binary).IsAssignableFrom(type) || typeof(FhirResponse).IsAssignableFrom(type);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            if (typeof(Binary).IsAssignableFrom(context.ObjectType) || typeof(FhirResponse).IsAssignableFrom(context.ObjectType))
            {
                Binary binary = null;
                if (typeof(FhirResponse).IsAssignableFrom(context.ObjectType))
                {
                    FhirResponse response = (FhirResponse)context.Object;

                    context.HttpContext.Response.AcquireHeaders(response);
                    context.HttpContext.Response.StatusCode = (int)response.StatusCode;

                    binary = response.Resource as Binary;
                }
                if (binary == null) return;

                context.HttpContext.Response.ContentType = binary.ContentType;

                Stream stream = new MemoryStream(binary.Content);
                await stream.CopyToAsync(context.HttpContext.Response.Body);
            }
        }
    }
}
#endif