/* 
 * Copyright (c) 2019-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
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
