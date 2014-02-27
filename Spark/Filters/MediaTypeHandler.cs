/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Spark.Support;
using Spark.Config;
using Hl7.Fhir.Rest;
using Spark.Core;
using Spark.Http;
using Hl7.Fhir.Model;
using System.Net;

namespace Spark.Filters
{
    public class MediaTypeHandler : DelegatingHandler
    {
        private bool isBinaryRequest(HttpRequestMessage request)
        {
            var ub = new UriBuilder(request.RequestUri);
            return ub.Path.Contains("Binary"); // todo: replace quick hack by solid solution.
        }

        private bool isTagRequest(HttpRequestMessage request)
        {
            var ub = new UriBuilder(request.RequestUri);
            return ub.Path.Contains("_tags"); // todo: replace quick hack by solid solution.
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // ballot: binary upload should be determined by the Content-Type header, instead of the Rest url?
            if (isBinaryRequest(request))
            {
                if (request.Content.Headers.ContentType != null)
                {
                    var format = request.Content.Headers.ContentType.MediaType;
                    request.Content.Headers.Replace("X-Content-Type", format);
                }

                request.Content.Headers.ContentType = new MediaTypeHeaderValue(FhirMediaType.BinaryResource);
                request.Headers.Replace("Accept", FhirMediaType.BinaryResource); // HACK
                // todo: HACK. passes to BinaryFhirFormatter
            }
            //else if (isTagRequest(request) && request.Method == HttpMethod.Delete)
            //{
            //    // EK: HACK DELETE _tag operations of type DELETE MUST have a body
            //    // Normally we would catch this in the controller, but the WebApi seems
            //    // to be confused when a DELETE with no body arrives while we have
            //    // a controller action with a [FromBody] parameter.
            //    var body = await request.Content.ReadAsByteArrayAsync();
            //    if (body == null || body.Length == 0)
            //        throw new SparkException(HttpStatusCode.BadRequest, "DELETE operation on _tags must have data in the body");
            //}
            else
            {
                // The requested response format can be overridden by the url parameter 'format'
                // Can only be json/xml (or equivalent MIME types) otherwise, ignore.
                string formatParam = request.Parameter("_format");
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
            }
            return await base.SendAsync(request, cancellationToken);
        }

        
    }

    
    // Instead of using the general purpose DelegatingHandler, could we use IContentNegotiator?
    public class FhirContentNegotiator : IContentNegotiator
    {
        public ContentNegotiationResult Negotiate(Type type, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters)
        {
            throw new NotImplementedException();
        }
    }

}
