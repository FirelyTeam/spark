﻿/* 
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
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Spark.Engine.Core;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Utility;
using Spark.Formatters;

namespace Spark.Engine.Extensions
{
    public static class HttpRequestFhirExtensions
    {
        public static void AcquireHeaders(this HttpResponseMessage response, FhirResponse fhirResponse)
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
                    Binary binary = fhir.Resource as Binary;
                    if (binary != null && request.IsRawBinaryRequest(typeof(Binary)))
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

        public static HttpResponseMessage CreateResponse(this HttpRequestMessage request, FhirResponse fhir)
        {
            HttpResponseMessage message = request.CreateBareFhirResponse(fhir);
            message.AcquireHeaders(fhir);
            return message;
        }

        public static DateTimeOffset? GetDateParameter(this HttpRequestMessage request, string name)
        {
            string param = request.GetParameter(name);
            if (param == null) return null;
            return DateTimeOffset.Parse(param);
        }

        public static int? GetIntParameter(this HttpRequestMessage request, string name)
        {
            string s = request.GetParameter(name);
            int n;
            return (int.TryParse(s, out n)) ? n : (int?)null;
        }

        public static bool? GetBooleanParameter(this HttpRequestMessage request, string name)
        {
            string s = request.GetParameter(name);           
            if(s == null) return null;
            try
            {
                bool b = PrimitiveTypeConverter.ConvertTo<bool>(s);
                return (bool.TryParse(s, out b)) ? b : (bool?)null;
            }
            catch
            {
                return null;
            }
        }

        public static DateTimeOffset? IfModifiedSince(this HttpRequestMessage request)
        {
            return request.Headers.IfModifiedSince;
        }

        public static IEnumerable<string> IfNoneMatch(this HttpRequestMessage request)
        {
            // The if-none-match can be either '*' or tags. This needs further implementation.
            return request.Headers.IfNoneMatch.Select(h => h.Tag);
        }

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

        public static string GetValue(this HttpRequestMessage request, string key)
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

        public static bool PreferRepresentation(this HttpRequestMessage request)
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

        public static SummaryType RequestSummary(this HttpRequestMessage request)
        {
            SummaryType? summaryType = null;
            string summary = request.GetParameter("_summary");
            if (string.IsNullOrWhiteSpace(summary))
                summaryType = SummaryType.False;
            else
                summaryType = EnumUtility.ParseLiteral<SummaryType>(summary, true);

            return summaryType.HasValue ? summaryType.Value : SummaryType.False;
        }


        /// <summary>
        /// Transfers the id to the <see cref="Resource"/>.
        /// </summary>
        /// <param name="request">An instance of <see cref="HttpRequestMessage"/>.</param>
        /// <param name="resource">An instance of <see cref="Resource"/>.</param>
        /// <param name="id">A <see cref="string"/> containing the id to transfer to Resource.Id.</param>
        public static void TransferResourceIdIfRawBinary(this HttpRequestMessage request, Resource resource, string id)
        {
            if (resource is Binary && resource.Id == null && id != null)
            {
                string contentType = request.GetContentTypeHeaderValue();
                if (!ContentType.XML_CONTENT_HEADERS.Contains(contentType) && !ContentType.JSON_CONTENT_HEADERS.Contains(contentType))
                    resource.Id = id;
            }
        }

        /// <summary>
        /// Returns true if the Accept header matches any of the FHIR supported Xml or Json MIME types, otherwise false.
        /// </summary>
        /// <param name="content">An instance of <see cref="HttpRequestMessage"/>.</param>
        /// <returns>Returns true if the Accept header matches any of the FHIR supported Xml or Json MIME types, otherwise false.</returns>
        public static bool IsAcceptHeaderFhirMediaType(this HttpRequestMessage request)
        {
            string accept = request.GetAcceptHeaderValue();
            return ContentType.XML_CONTENT_HEADERS.Contains(accept)
                || ContentType.JSON_CONTENT_HEADERS.Contains(accept);
        }
        
        public static bool IsRawBinaryRequest(this HttpRequestMessage request, Type type)
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

        public static bool IsRawBinaryPostOrPutRequest(this HttpRequestMessage request)
        {
            var ub = new UriBuilder(request.RequestUri);
            // TODO: KM: Path matching is not optimal should be replaced by a more solid solution.
            return ub.Path.Contains("Binary") 
                && !request.Content.IsContentTypeHeaderFhirMediaType()
                && (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put);
        }
    }
}