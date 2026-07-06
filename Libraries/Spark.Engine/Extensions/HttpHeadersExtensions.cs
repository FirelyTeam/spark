/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Spark.Engine.Extensions;

public static class HttpRequestExtensions
{
    /// <summary>
    /// Returns true if contentType matches any of the supported Xml or Json MIME types.
    /// </summary>
    /// <param name="contentType">The value from the Content-Type header</param>
    /// <returns>Returns true if contentType matches any of the supported Xml or Json MIME types.</returns>
    public static bool IsContentTypeHeaderFhirMediaType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return false;

        if (MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue mediaTypeHeaderValue))
            contentType = mediaTypeHeaderValue.MediaType;

        return ContentType.XML_CONTENT_HEADERS.Contains(contentType)
               || ContentType.JSON_CONTENT_HEADERS.Contains(contentType);
    }

    public static string GetParameter(this HttpRequest request, string key)
    {
        string value = null;
        if(request.Query.ContainsKey(key))
            value = request.Query.FirstOrDefault(p => p.Key == key).Value.FirstOrDefault();
        return value;
    }

    public static List<Tuple<string, string>> TupledParameters(this HttpRequest request)
    {
        return UriParamList.FromQueryString(request.QueryString.Value);
    }

    public static SearchParams GetSearchParamsFromBody(this HttpRequest request)
    {
        var list = new List<Tuple<string, string>>();
        foreach (var parameter in request.Form)
        {
            list.AddRange(parameter.Value.Select(value => new Tuple<string, string>(parameter.Key, value)));
        }
        return request.GetSearchParams().AddAll(list);
    }

    public static SearchParams GetSearchParams(this HttpRequest request)
    {
        var parameters = request.TupledParameters().Where(tp => tp.Item1 != "_format");
        var searchCommand = SearchParams.FromUriParamList(parameters);
        return searchCommand;
    }
}

public static class FhirHttpHeaders
{
    public const string IfNoneExist = "If-None-Exist";
}
