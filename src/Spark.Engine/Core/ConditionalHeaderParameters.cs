/* 
 * Copyright (c) 2015-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Net.Http;
using Spark.Engine.Extensions;
#if NETSTANDARD2_0 || NET6_0
using Microsoft.AspNetCore.Http;
#endif

namespace Spark.Engine.Core
{
    public class ConditionalHeaderParameters
    {
        public ConditionalHeaderParameters(HttpRequestMessage request)
        {
            IfNoneMatchTags = request.IfNoneMatch();
            IfModifiedSince = request.IfModifiedSince();
        }

#if NETSTANDARD2_0 || NET6_0
        public ConditionalHeaderParameters(HttpRequest request)
        {
            IfNoneMatchTags = request.IfNoneMatch();
            IfModifiedSince = request.IfModifiedSince();
        }
#endif

        public IEnumerable<string> IfNoneMatchTags { get; set; }
        public DateTimeOffset? IfModifiedSince { get; set; }
    }
}