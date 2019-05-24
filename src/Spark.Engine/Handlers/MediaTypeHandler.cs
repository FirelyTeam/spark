﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Rest;
using Spark.Engine.Extensions;
using Spark.Engine.Core;
using System.Linq;

namespace Spark.Handlers
{
    public class FhirMediaTypeHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string formatParam = request.GetParameter("_format");
            if (!string.IsNullOrEmpty(formatParam))
            {
                var accepted = ContentType.GetResourceFormatFromFormatParam(formatParam);
                if (accepted != ResourceFormat.Unknown)
                {
                    request.Headers.Accept.Clear();

                    if (accepted == ResourceFormat.Json)
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ContentType.JSON_CONTENT_HEADER));
                    else
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ContentType.XML_CONTENT_HEADER));
                }
            }

            // BALLOT: binary upload should be determined by the Content-Type header, instead of the Rest url?
            // HACK: passes to BinaryFhirFormatter
            if (request.IsRawBinaryPostOrPutRequest())
            {
                if (!request.Content.IsContentTypeHeaderFhirMediaType())
                {
                    var format = request.Content.Headers.ContentType.MediaType;
                    request.Content.Headers.Replace("X-Content-Type", format);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue(FhirMediaType.OCTET_STREAM_CONTENT_HEADER);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
