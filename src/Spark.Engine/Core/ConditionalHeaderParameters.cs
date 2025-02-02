/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Net.Http;
using Spark.Engine.Extensions;
#if NETSTANDARD2_0 || NET6_0_OR_GREATER
using Microsoft.AspNetCore.Http;
#endif

namespace Spark.Engine.Core;

public class ConditionalHeaderParameters
{
    public ConditionalHeaderParameters(HttpRequestMessage request)
    {
        IfNoneMatchTags = request.IfNoneMatch();
        IfModifiedSince = request.IfModifiedSince();
    }

#if NETSTANDARD2_0 || NET6_0_OR_GREATER
        public ConditionalHeaderParameters(HttpRequest request)
        {
            IfNoneMatchTags = request.IfNoneMatch();
            IfModifiedSince = request.IfModifiedSince();
        }
#endif

    public IEnumerable<string> IfNoneMatchTags { get; set; }
    public DateTimeOffset? IfModifiedSince { get; set; }
}
