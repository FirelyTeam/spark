/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly)
 * Copyright (c) 2020-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Collections;
using Hl7.Fhir.Model;

namespace Spark.Search.Support
{
    public static class NullExtensions
    {
        public static bool IsNullOrEmpty(this IList list)
        {
            if (list == null) return true;

            return list.Count == 0;
        }

        public static bool IsNullOrEmpty(this PrimitiveType element)
        {
            if (element == null) return true;

            if (element.ObjectValue == null) return true;

            return true;
        }
    }
}
