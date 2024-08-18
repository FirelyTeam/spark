/* 
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Engine.Extensions
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset TruncateToMillis(this DateTimeOffset dateTime)
        {
            return dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerMillisecond));
        }
    }
}
