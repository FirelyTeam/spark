/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Utility;
using Spark.Engine.Utility;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

[assembly: InternalsVisibleTo("Spark.Engine.Test")]
namespace Spark.Engine.Extensions;

public static class HttpRequestFhirExtensions
{
    private static string WithoutQuotes(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return null;
        }
        else
        {
            return s.Trim('"');
        }
    }

    public static int GetPagingOffsetParameter(this HttpRequest request)
    {
        var offset = FhirParameterParser.ParseIntParameter(request.GetParameter(FhirParameter.OFFSET));
        if (!offset.HasValue)
        {
            // This part is kept as backwards compatibility for the "start" parameter which was used as an offset
            // in earlier versions of Spark.
            offset = FhirParameterParser.ParseIntParameter(request.GetParameter(FhirParameter.SNAPSHOT_INDEX));
        }

        return offset.HasValue ? offset.Value : 0;
    }

    internal static string GetRequestUri(this HttpRequest request)
    {
        var httpRequestFeature = request.HttpContext.Features.Get<IHttpRequestFeature>();
        return $"{request.Scheme}://{request.Host}{httpRequestFeature.Path}";
    }

    internal static DateTimeOffset? IfModifiedSince(this HttpRequest request)
    {
        request.Headers.TryGetValue("If-Modified-Since", out StringValues values);
        if (!DateTimeOffset.TryParse(values.FirstOrDefault(), out DateTimeOffset modified)) return null;
        return modified;
    }

    internal static IEnumerable<string> IfNoneMatch(this HttpRequest request)
    {
        if (!request.Headers.TryGetValue("If-None-Match", out StringValues values)) return new string[0];
        return values.ToArray();
    }

    public static string IfMatchVersionId(this HttpRequest request)
    {
        if (request.Headers.Count == 0) return null;

        if (!request.Headers.TryGetValue("If-Match", out StringValues value)) return null;
        var tag = value.FirstOrDefault();
        if (tag == null) return null;
        return WithoutQuotes(tag);
    }

    internal static SummaryType RequestSummary(this HttpRequest request)
    {
        request.Query.TryGetValue("_summary", out StringValues stringValues);
        return GetSummary(stringValues.FirstOrDefault());
    }

    /// <summary>
    /// Transfers the id to the <see cref="Resource"/>.
    /// </summary>
    /// <param name="request">An instance of <see cref="HttpRequest"/>.</param>
    /// <param name="resource">An instance of <see cref="Resource"/>.</param>
    /// <param name="id">A <see cref="string"/> containing the id to transfer to Resource.Id.</param>
    public static void TransferResourceIdIfRawBinary(this HttpRequest request, Resource resource, string id)
    {
        if (request.Headers.TryGetValue("Content-Type", out StringValues value))
        {
            string contentType = value.FirstOrDefault();
            TransferResourceIdIfRawBinary(contentType, resource, id);
        }
    }

    public static string IfNoneExist(this RequestHeaders headers)
    {
        string ifNoneExist = null;
        if (headers.Headers.TryGetValue(FhirHttpHeaders.IfNoneExist, out StringValues values))
        {
            ifNoneExist = values.FirstOrDefault();
        }
        return ifNoneExist;
    }

    /// <summary>
    /// Returns true if the Accept header matches any of the FHIR supported Xml or Json MIME types, otherwise false.
    /// </summary>
    private static bool IsAcceptHeaderFhirMediaType(this HttpRequest request)
    {
        var acceptHeader = request.GetTypedHeaders().Accept.FirstOrDefault();
        if (acceptHeader == null || acceptHeader.MediaType == StringSegment.Empty)
            return false;

        string accept = acceptHeader.MediaType.Value;
        return ContentType.XML_CONTENT_HEADERS.Contains(accept)
            || ContentType.JSON_CONTENT_HEADERS.Contains(accept);
    }

    internal static bool IsRawBinaryRequest(this OutputFormatterCanWriteContext context, Type type)
    {
        if (type == typeof(Binary) || (type == typeof(FhirResponse)) && ((FhirResponse)context.Object).Resource is Binary)
        {
            HttpRequest request = context.HttpContext.Request;
            bool isFhirMediaType = false;
            if (request.Method == "GET")
                isFhirMediaType = request.IsAcceptHeaderFhirMediaType();
            else if (request.Method == "POST" || request.Method == "PUT")
                isFhirMediaType = HttpRequestExtensions.IsContentTypeHeaderFhirMediaType(request.ContentType);

            var ub = new UriBuilder(request.GetRequestUri());
            // TODO: KM: Path matching is not optimal should be replaced by a more solid solution.
            return ub.Path.Contains("Binary")
                && !isFhirMediaType;
        }
        else
            return false;
    }

    internal static bool IsRawBinaryRequest(this HttpRequest request)
    {
        var ub = new UriBuilder(request.GetRequestUri());
        return ub.Path.Contains("Binary")
            && !ub.Path.EndsWith("_search");
    }

    internal static bool IsRawBinaryPostOrPutRequest(this HttpRequest request)
    {
        var ub = new UriBuilder(request.GetRequestUri());
        // TODO: KM: Path matching is not optimal should be replaced by a more solid solution.
        return ub.Path.Contains("Binary")
            && !ub.Path.EndsWith("_search")
            && !HttpRequestExtensions.IsContentTypeHeaderFhirMediaType(request.ContentType)
            && (request.Method == "POST" || request.Method == "PUT");
    }

    internal static void AcquireHeaders(this HttpResponse response, FhirResponse fhirResponse)
    {
        if (fhirResponse.Key != null)
        {
            response.Headers.Append(HttpHeaderName.ETAG, ETag.Create(fhirResponse.Key.VersionId)?.ToString());

            Uri location = fhirResponse.Key.ToUri();
            response.Headers.Append(HttpHeaderName.LOCATION, location.OriginalString);

            if (response.ContentLength > 0)
            {
                response.Headers.Append(HttpHeaderName.CONTENT_LOCATION, location.OriginalString);
                if (fhirResponse.Resource?.Meta?.LastUpdated != null)
                {
                    response.Headers.Append(HttpHeaderName.LAST_MODIFIED,
                        fhirResponse.Resource.Meta.LastUpdated.Value.ToString("R"));
                }
            }
        }
    }

    private static SummaryType GetSummary(string summary)
    {
        SummaryType? summaryType;
        if (string.IsNullOrWhiteSpace(summary))
            summaryType = SummaryType.False;
        else
            summaryType = EnumUtility.ParseLiteral<SummaryType>(summary, true);

        return summaryType ?? SummaryType.False;
    }

    private static void TransferResourceIdIfRawBinary(string contentType, Resource resource, string id)
    {
        if (!string.IsNullOrEmpty(contentType) && resource is Binary && resource.Id == null && id != null)
        {
            if (!ContentType.XML_CONTENT_HEADERS.Contains(contentType) && !ContentType.JSON_CONTENT_HEADERS.Contains(contentType))
                resource.Id = id;
        }
    }
}
