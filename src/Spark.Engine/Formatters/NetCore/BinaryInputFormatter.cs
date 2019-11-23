#if NETSTANDARD2_0
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Primitives;
using Spark.Core;
using Spark.Engine.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Spark.Engine.Formatters
{
    public class BinaryInputFormatter : InputFormatter
    {
        public BinaryInputFormatter()
        {
            SupportedMediaTypes.Add(FhirMediaType.OCTET_STREAM_CONTENT_HEADER);
        }

        protected override bool CanReadType(Type type)
        {
            return type == typeof(Resource);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue("X-Content-Type", out StringValues contentTypeHeaderValues))
                throw Error.BadRequest("Binary POST and PUT must provide a Content-Type header.");

            string contentType = contentTypeHeaderValues.FirstOrDefault();
            MemoryStream memoryStream = new MemoryStream((int) context.HttpContext.Request.Body.Length);
            await context.HttpContext.Request.Body.CopyToAsync(memoryStream);
            Binary binary = new Binary
            {
                ContentType = contentType,
                Content = memoryStream.ToArray()
            };

            return await InputFormatterResult.SuccessAsync(binary);
        }
    }
}
#endif