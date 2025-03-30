/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Rest;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Core;

public static class FhirMediaType
{
    public static string OctetStreamMimeType = "application/octet-stream";
    public static string FormUrlEncodedMimeType = "application/x-www-form-urlencoded";
    public static string AnyMimeType = "*/*";

    public static IEnumerable<string> JsonMimeTypes => ContentType.JSON_CONTENT_HEADERS;
    public static IEnumerable<string> XmlMimeTypes => ContentType.XML_CONTENT_HEADERS;
    public static IEnumerable<string> SupportedMimeTypes => JsonMimeTypes
        .Concat(XmlMimeTypes)
        .Concat(new[] { OctetStreamMimeType, FormUrlEncodedMimeType, AnyMimeType });
}
