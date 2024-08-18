/* 
 * Copyright (c) 2019-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

#if NETSTANDARD2_0 || NET6_0
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
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
                    if (context.Request.Headers.ContainsKey(HttpHeaderName.ACCEPT)) context.Request.Headers.Remove(HttpHeaderName.ACCEPT);
                    if (accepted == ResourceFormat.Json)
                        context.Request.Headers.Add(HttpHeaderName.ACCEPT, new StringValues(ContentType.JSON_CONTENT_HEADER));
                    else
                        context.Request.Headers.Add(HttpHeaderName.ACCEPT, new StringValues(ContentType.XML_CONTENT_HEADER));
                }
            }

            if (context.Request.IsRawBinaryPostOrPutRequest())
            {
                if (!HttpRequestExtensions.IsContentTypeHeaderFhirMediaType(context.Request.ContentType))
                {
                    string contentType = context.Request.ContentType;
                    context.Request.Headers.Add(HttpHeaderName.X_CONTENT_TYPE, contentType);
                    context.Request.ContentType = FhirMediaType.OctetStreamMimeType;
                }
            }

            await _next(context);
        }
    }
}
#endif