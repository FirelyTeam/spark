#if NETSTANDARD2_0
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace Spark.Engine.Handlers.NetCore
{
    public class FormatTypeHandler
    {
        private readonly RequestDelegate _next;

        public FormatTypeHandler(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string format = context.Request.GetParameter("_format");
            if (!string.IsNullOrEmpty(format))
            {
                ResourceFormat accepted = ContentType.GetResourceFormatFromFormatParam(format);
                if (accepted != ResourceFormat.Unknown)
                {
                    if (context.Request.Headers.ContainsKey("Accept")) context.Request.Headers.Remove("Accept");
                    if (accepted == ResourceFormat.Json)
                        context.Request.Headers.Add("Accept", new StringValues(ContentType.JSON_CONTENT_HEADER));
                    else
                        context.Request.Headers.Add("Accept", new StringValues(ContentType.XML_CONTENT_HEADER));
                }
            }

            if (context.Request.IsRawBinaryPostOrPutRequest())
            {
                if (!HttpRequestExtensions.IsContentTypeHeaderFhirMediaType(context.Request.ContentType))
                {
                    string contentType = context.Request.ContentType;
                    context.Request.Headers.Add("X-Content-Type", contentType);
                    context.Request.ContentType = FhirMediaType.OCTET_STREAM_CONTENT_HEADER;
                }
            }

            //application/foobar
            if (context.Request.Headers.ContainsKey("Accept"))
            {
                var acceptHeader = context.Request.Headers["Accept"].ToString();
                if (!MimeType.SupportedMimeTypes.Any(mimeType => acceptHeader.Contains(mimeType)))
                {
                    throw Error.NotAcceptable();
                }
            }
            if(context.Request.ContentType != null)
            {
                if (!MimeType.SupportedMimeTypes.Any(mimeType => context.Request.ContentType.Contains(mimeType)))
                {
                    throw Error.UnsupportedMediaType();
                }
            }

            await _next(context);
        }
    }
}
#endif