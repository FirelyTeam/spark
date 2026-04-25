/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using Spark.Engine.Extensions;
using Spark.Engine.Utility;
using Microsoft.AspNetCore.Http;

namespace Spark.Engine.Core;

public class HistoryParameters
{
    public HistoryParameters(HttpRequest request)
    {
        Count = FhirParameterParser.ParseIntParameter(request.GetParameter(FhirParameter.COUNT));
        Since = FhirParameterParser.ParseDateParameter(request.GetParameter(FhirParameter.SINCE));
        SortBy = request.GetParameter(FhirParameter.SORT);
    }

    public int? Count { get; set; }
    public DateTimeOffset? Since { get; set; }
    public string Format { get; set; }
    public string SortBy { get; set; }
}
