/* 
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Engine.Extensions;

public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset TruncateToMillis(this DateTimeOffset dateTime)
    {
        return dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerMillisecond));
    }
}
