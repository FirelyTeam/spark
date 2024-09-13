/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Utility;
using Spark.Engine.Utility;
using Spark.Formatters;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
#if NETSTANDARD2_0 || NET6_0
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
#endif

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

#if NETSTANDARD2_0 || NET6_0
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
                response.Headers.Add(HttpHeaderName.ETAG, ETag.Create(fhirResponse.Key.VersionId)?.ToString());

                Uri location = fhirResponse.Key.ToUri();
                response.Headers.Add(HttpHeaderName.LOCATION, location.OriginalString);

                if (response.Body != null)
                {
                    response.Headers.Add(HttpHeaderName.CONTENT_LOCATION, location.OriginalString);
                    if (fhirResponse.Resource?.Meta?.LastUpdated != null)
                    {
                        response.Headers.Add(HttpHeaderName.LAST_MODIFIED, fhirResponse.Resource.Meta.LastUpdated.Value.ToString("R"));
                    }
                }
            }
        }

#endif

    public static int GetPagingOffsetParameter(this HttpRequestMessage request)
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

    internal static void AcquireHeaders(this HttpResponseMessage response, FhirResponse fhirResponse)
    {
        if (fhirResponse.Key != null)
        {
            response.Headers.ETag = ETag.Create(fhirResponse.Key.VersionId);

            Uri location = fhirResponse.Key.ToUri();
            response.Headers.Location = location;

            if (response.Content != null)
            {
                response.Content.Headers.ContentLocation = location;
                if (fhirResponse.Resource != null && fhirResponse.Resource.Meta != null)
                {
                    response.Content.Headers.LastModified = fhirResponse.Resource.Meta.LastUpdated;
                }
                if(fhirResponse.Resource is Binary)
                {
                    response.Content.Headers.Add(HttpHeaderName.CONTENT_DISPOSITION, "attachment");
                }
            }
        }
    }

    private static HttpResponseMessage CreateBareFhirResponse(this HttpRequestMessage request, FhirResponse fhir)
    {
        bool includebody = request.PreferRepresentation();

        if (fhir.Resource != null)
        {
            if (includebody)
            {
                if (fhir.Resource is Binary binary && request.IsRawBinaryRequest(typeof(Binary)))
                {
                    return request.CreateResponse(fhir.StatusCode, binary, new BinaryFhirFormatter(), binary.ContentType);
                }
                else
                {
                    return request.CreateResponse(fhir.StatusCode, fhir.Resource);
                }
            }
            else
            {
                return request.CreateResponse(fhir.StatusCode);
            }
        }
        else
        {
            return request.CreateResponse(fhir.StatusCode);
        }
    }

    internal static HttpResponseMessage CreateResponse(this HttpRequestMessage request, FhirResponse fhir)
    {
        HttpResponseMessage message = request.CreateBareFhirResponse(fhir);
        message.AcquireHeaders(fhir);
        return message;
    }

    internal static DateTimeOffset? IfModifiedSince(this HttpRequestMessage request)
    {
        return request.Headers.IfModifiedSince;
    }

    internal static IEnumerable<string> IfNoneMatch(this HttpRequestMessage request)
    {
        // The if-none-match can be either '*' or tags. This needs further implementation.
        return request.Headers.IfNoneMatch.Select(h => h.Tag);
    }

    public static string GetParameter(this HttpRequestMessage request, string key)
    {
        NameValueCollection queryNameValuePairs = request.RequestUri.ParseQueryString();
        foreach (var currentKey in queryNameValuePairs.AllKeys)
        {
            if (currentKey == key) return queryNameValuePairs[currentKey];
        }
        return null;
    }

    public static List<Tuple<string, string>> TupledParameters(this HttpRequestMessage request)
    {
        return UriParamList.FromQueryString(request.RequestUri.Query);
    }

    private static string GetValue(this HttpRequestMessage request, string key)
    {
        if (request.Headers.Count() > 0)
        {
            if (request.Headers.TryGetValues(key, out IEnumerable<string> values))
            {
                string value = values.FirstOrDefault();
                return value;
            }
            return null;
        }
        else return null;
    }

    private static bool PreferRepresentation(this HttpRequestMessage request)
    {
        string value = request.GetValue("Prefer");
        return (value == "return=representation" || value == null);
    }

    public static string IfMatchVersionId(this HttpRequestMessage request)
    {
        if (request.Headers.Count() > 0)
        {
            var tag = request.Headers.IfMatch.FirstOrDefault();
            if (tag != null)
            {
                return WithoutQuotes(tag.Tag);
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
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

    internal static SummaryType RequestSummary(this HttpRequestMessage request)
    {
        string summary = request.GetParameter("_summary");
        return GetSummary(summary);
    }

    /// <summary>
    /// Transfers the id to the <see cref="Resource"/>.
    /// </summary>
    /// <param name="request">An instance of <see cref="HttpRequestMessage"/>.</param>
    /// <param name="resource">An instance of <see cref="Resource"/>.</param>
    /// <param name="id">A <see cref="string"/> containing the id to transfer to Resource.Id.</param>
    public static void TransferResourceIdIfRawBinary(this HttpRequestMessage request, Resource resource, string id)
    {
        string contentType = request.GetContentTypeHeaderValue();
        TransferResourceIdIfRawBinary(contentType, resource, id);
    }

    private static void TransferResourceIdIfRawBinary(string contentType, Resource resource, string id)
    {
        if (!string.IsNullOrEmpty(contentType) && resource is Binary && resource.Id == null && id != null)
        {
            if (!ContentType.XML_CONTENT_HEADERS.Contains(contentType) && !ContentType.JSON_CONTENT_HEADERS.Contains(contentType))
                resource.Id = id;
        }
    }

    /// <summary>
    /// Returns true if the Accept header matches any of the FHIR supported Xml or Json MIME types, otherwise false.
    /// </summary>
    /// <param name="content">An instance of <see cref="HttpRequestMessage"/>.</param>
    /// <returns>Returns true if the Accept header matches any of the FHIR supported Xml or Json MIME types, otherwise false.</returns>
    private static bool IsAcceptHeaderFhirMediaType(this HttpRequestMessage request)
    {
        string accept = request.GetAcceptHeaderValue();
        return ContentType.XML_CONTENT_HEADERS.Contains(accept)
               || ContentType.JSON_CONTENT_HEADERS.Contains(accept);
    }

    internal static bool IsRawBinaryRequest(this HttpRequestMessage request, Type type)
    {
        if (type == typeof(Binary) || type == typeof(FhirResponse))
        {
            bool isFhirMediaType = false;
            if (request.Method == HttpMethod.Get)
                isFhirMediaType = request.IsAcceptHeaderFhirMediaType();
            else if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put)
                isFhirMediaType = request.Content.IsContentTypeHeaderFhirMediaType();

            var ub = new UriBuilder(request.RequestUri);
            // TODO: KM: Path matching is not optimal should be replaced by a more solid solution.
            return ub.Path.Contains("Binary")
                   && !isFhirMediaType;
        }
        else
            return false;
    }

    internal static bool IsRawBinaryPostOrPutRequest(this HttpRequestMessage request)
    {
        var ub = new UriBuilder(request.RequestUri);
        // TODO: KM: Path matching is not optimal should be replaced by a more solid solution.
        return ub.Path.Contains("Binary")
               && !ub.Path.EndsWith("_search")
               && !request.Content.IsContentTypeHeaderFhirMediaType()
               && (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put);
    }
}
