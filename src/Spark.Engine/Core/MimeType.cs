/* 
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Rest;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Core
{
    internal static class MimeType
    {
        public static string DefaultJsonMimeType = ContentType.JSON_CONTENT_HEADER;
        public static string DefaultXmlMimeType = ContentType.XML_CONTENT_HEADER;
        public static IEnumerable<string> JsonMimeTypes => ContentType.JSON_CONTENT_HEADERS;
        public static IEnumerable<string> XmlMimeTypes => ContentType.XML_CONTENT_HEADERS;
        public static IEnumerable<string> SupportedMimeTypes => JsonMimeTypes
            .Concat(XmlMimeTypes)
            .Concat(new[] { "application/octet-stream", "application/x-www-form-urlencoded", "*/*" });
    }
}
