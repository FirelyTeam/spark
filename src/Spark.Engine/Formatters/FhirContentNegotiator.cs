/* 
 * Copyright (c) 2019-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Rest;
using Spark.Engine.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;

namespace Spark.Formatters
{
    public class FhirContentNegotiator : DefaultContentNegotiator
    {
        public override ContentNegotiationResult Negotiate(Type type, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters)
        {
            MediaTypeFormatter formatter;
            if(request.IsRawBinaryRequest(type))
            {
                formatter = formatters.Where(f => f is BinaryFhirFormatter).SingleOrDefault();
                if (formatter != null) return new ContentNegotiationResult(formatter.GetPerRequestFormatterInstance(type, request, null), null);
            }

            return base.Negotiate(type, request, formatters);
        }
    }
}