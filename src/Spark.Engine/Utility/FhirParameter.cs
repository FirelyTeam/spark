/* 
 * Copyright (c) 2019-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Engine.Utility
{
    public static class FhirParameterParser
    {
        public static DateTimeOffset? ParseDateParameter(string value)
        {
            return DateTimeOffset.TryParse(value, out var dateTime)
                ? dateTime : (DateTimeOffset?)null;
        }

        public static int? ParseIntParameter(string value)
        {
            return (int.TryParse(value, out int n)) ? n : default(int?);
        }
    }
}
