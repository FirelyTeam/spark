#if NETSTANDARD2_0
using FhirModel = Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc.Formatters;
using Spark.Engine.Core;
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
            return type == typeof(FhirModel.Binary) || type == typeof(FhirResponse);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            if (typeof(FhirModel.Binary).IsAssignableFrom(context.ObjectType))
            {
                FhirModel.Binary binary = (FhirModel.Binary)context.Object;
                var stream = new MemoryStream(binary.Content);
                context.HttpContext.Response.Headers.Add("Content-Type", binary.ContentType);
                await stream.CopyToAsync(context.HttpContext.Response.Body);
            }
        }
    }
}
#endif