/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using Spark.Engine.Extensions;
using Microsoft.AspNetCore.Http;

namespace Spark.Engine.Core;

public class ConditionalHeaderParameters
{
    public ConditionalHeaderParameters(HttpRequest request)
    {
        IfNoneMatchTags = request.IfNoneMatch();
        IfModifiedSince = request.IfModifiedSince();
    }

    public IEnumerable<string> IfNoneMatchTags { get; set; }
    public DateTimeOffset? IfModifiedSince { get; set; }
}
