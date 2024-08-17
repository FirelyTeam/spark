/* 
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
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
